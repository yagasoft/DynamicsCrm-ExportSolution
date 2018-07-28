#region Imports

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using LinkDev.Libraries.Common;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#endregion

namespace Yagasoft.Tools.ExportSolution
{
	/// <summary>
	///     Author: Ahmed el-Sawalhy (yagasoft.com)
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			var log = new CrmLog(true, LogLevel.Debug);
			log.InitOfflineLog("log.csv", false,
				new FileConfiguration
				{
					FileSplitMode = SplitMode.Size,
					MaxFileSize = 1024,
					FileDateFormat = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}"
				});

			try
			{
				var settings = GetConfigurationParams(args);

				var service = ConnectToCrm(settings.ConnectionString, log);

				if (service == null)
				{
					return;
				}

				foreach (var solutionConfig in settings.SolutionConfigs)
				{
					try
					{
						var (solutionVersion, exportXml) = RetrieveSolution(solutionConfig.SolutionName, log, service);
						var outputPath = string.IsNullOrWhiteSpace(solutionConfig.OutputPath)
							? settings.DefaultOutputPath
							: solutionConfig.OutputPath;
						var fullPath = BuildOutputPath(outputPath, solutionConfig.OutputFilename,
							solutionConfig.SolutionName, solutionVersion);
						Directory.CreateDirectory(outputPath);
						log.Log($"Writing solution to '{fullPath}'...");
						File.WriteAllBytes($"{fullPath}", exportXml);
						log.Log($"Solution file written.");
					}
					catch (Exception e)
					{
						log.Log(e);
					}
				}
			}
			catch (Exception e)
			{
				log.Log(e);
				log.ExecutionFailed();
			}
			finally
			{
				log.LogExecutionEnd();
			}
		}

		private static Settings GetConfigurationParams(string[] args = null)
		{
			Settings settings;

			if (args?.Any() == true)
			{
				var settingsPath = args[0];

				if (!File.Exists(settingsPath))
				{
					throw new FileNotFoundException("Couldn't find settings file.", settingsPath);
				}

				var settingsJson = File.ReadAllText(settingsPath);
				settings = JsonConvert.DeserializeObject<Settings>(settingsJson,
					new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
			}
			else
			{
				var keys = new[] { "ConnectionString", "SolutionNames", "OutputPath", "OutputFilename" };

				foreach (var key in keys)
				{
					if (!ConfigurationManager.AppSettings.AllKeys.Contains($"{key}"))
					{
						throw new KeyNotFoundException($"{key} is missing in configuration file.");
					}
				}

				settings =
					new Settings
					{
						ConnectionString = ConfigurationManager.AppSettings["ConnectionString"],
						SolutionConfigs = ConfigurationManager.AppSettings["SolutionNames"].Trim(',').Split(',')
							.Select(solutionName =>
							new SolutionConfig
							{
								SolutionName = solutionName,
								OutputPath = ConfigurationManager.AppSettings["OutputPath"],
								OutputFilename = ConfigurationManager.AppSettings["OutputFilename"]
							}).ToList()
					};
			}

			return settings;
		}

		private static CrmServiceClient ConnectToCrm(string connectionString, CrmLog log)
		{
			log.Log($"Connecting to '{connectionString}' ...");
			var service = new CrmServiceClient(connectionString);

			if (!string.IsNullOrWhiteSpace(service.LastCrmError) || service.LastCrmException != null)
			{
				log.LogError($"Failed to connect => {service.LastCrmError}");

				if (service.LastCrmException != null)
				{
					throw service.LastCrmException;
				}

				return null;
			}

			log.Log($"Connected!");
			return service;
		}

		private static (string solutionVersion, byte[] exportXml) RetrieveSolution(string solutionName, CrmLog log,
			CrmServiceClient service)
		{
			var query =
				new QueryExpression
				{
					EntityName = Solution.EntityLogicalName,
					ColumnSet = new ColumnSet(Solution.Fields.Version),
					Criteria = new FilterExpression()
				};
			query.Criteria.AddCondition(Solution.Fields.Name, ConditionOperator.Equal, solutionName);

			log.Log($"Retrieving solution version for solution '{solutionName}'...");
			var solution = service.RetrieveMultiple(query).Entities.FirstOrDefault()?.ToEntity<Solution>();
			log.Log($"Version: {solution?.Version}.");

			if (solution == null)
			{
				throw new NotFoundException("Couldn't find solution in CRM.");
			}

			var request =
				new ExportSolutionRequest
				{
					Managed = false,
					SolutionName = solutionName
				};

			log.Log($"Exporting solution '{solutionName}'...");
			var response = (ExportSolutionResponse)service.Execute(request);
			log.Log($"Exported!");

			var exportXml = response.ExportSolutionFile;

			return (solution.Version, exportXml);
		}

		private static string BuildOutputPath(string outputPath, string outputFilename, string solutionName,
			string solutionVersion)
		{
			string filename;
			var fullPath = outputPath.Trim('\\');

			if (outputFilename.IsNotEmpty())
			{
				filename = outputFilename;
				var nameNoExtension = Path.GetFileNameWithoutExtension(filename);
				var extension = Path.GetExtension(filename);

				if (File.Exists($"{fullPath}\\{filename}"))
				{
					var suffix = 1;
					string tempPath;

					do
					{
						tempPath = $"{fullPath}\\{nameNoExtension}-{suffix++}.{extension}";
					}
					while (File.Exists(tempPath));

					fullPath = tempPath;
				}
				else
				{
					fullPath += $"\\{filename}";
				}
			}
			else
			{
				filename = $"{solutionName}_{solutionVersion.Replace('.', '_')}_-_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";
				fullPath += $"\\{filename}";
			}

			return fullPath;
		}
	}

	class Settings
	{
		public string ConnectionString;
		public string DefaultOutputPath;
		public List<SolutionConfig> SolutionConfigs;
	}

	class SolutionConfig
	{
		public string SolutionName;
		public string OutputPath;
		public string OutputFilename;
	}
}
