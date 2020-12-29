# AlidnsSyncService
基于.Net 5编写的阿里dns后台同步服务，适用于Windows及Linux.

## 安装
1. 确保已安装.Net 5 Runtime;
2. 下载源码使用VS 2019编译或者直接下载已编译的二进制文件，解压（下面以"C:\AlidnsSyncService\"为例）;
3. 修改配置文件appsettings.json中的AccessKeyId和AccessKeySecret值为你的阿里云Key和Secret;
4. Powershell新建服务：
``` ps
New-Service -Name "AlidnsSync" -BinaryPathName "C:\AlidnsSyncService\AlidnsSyncService.exe" -Description "阿里dns后台同步服务" -StartupType "Automatic" -DisplayName "Alidns Sync Service"
```
5. 运行服务：
```ps
Start-Service -Name "AlidnsSync"
```
也可以直接在Windows服务管理中开启。

## 卸载
```
Stop-Service -Name "AlidnsSync"
Remove-Service -Name "AlidnsSync"
```
运行该命令需要Powershell 6.0以上版本。
