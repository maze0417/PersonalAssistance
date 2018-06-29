@echo off
runas /user:administrator /savecred "powershell %0\..\build.ps1 -configuration release %*"
