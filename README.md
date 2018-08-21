# DynamicsCrm-ExportSolution

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-ExportSolution](https://badges.gitter.im/yagasoft/DynamicsCrm-ExportSolution.svg)](https://gitter.im/yagasoft/DynamicsCrm-ExportSolution?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 1.1
---

A command-line tool that exports a list of solutions from CRM. Created for the purpose of backing-up the solution using Windows Task Scheduler.

### Guide

  + Configuration (either)
	+ App.config (Yagasoft.Tools.ExportSolution.exe.config)
	+ *.json
	  + Pass the filename as a command-line argument
  + Solution's name is appended with the current date, which is useful for automatic backups
  + 'OutputFileName' param is optional, and overrides the default solution naming
    + If the file already exists, a number suffix will be appended automatically

---
**Copyright &copy; by Ahmed el-Sawalhy ([YagaSoft](http://yagasoft.com))** -- _GPL v3 Licence_
