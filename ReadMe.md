# Azure.ADDS.UserWriteBack
Azure AD Sync is a helpful tool for the Hybrid cloud model and supporting the password writeback. However, if any reason your organization needs user writeback functionality, then Azure AD Sync itself cannot make it.

I developed this tool for user writeback functionality.
This tool allows you to config which users in which Azure group you want to write back and which OU and AD group you would like users to be in as well.

## How to use this tool?
1. Create an Application Register (ex: the name is azure-adds-userwriteback) with Microsoft Graph API permission is **Group.Read.All** and **User.Read.All**
2. Update the information to appsettings.json as below
   ```json
   {
       "AzAdSync": {
          "Authority": "https://login.microsoftonline.com/[TenantId]",
          "ClientId": "[ClientId]",
          "ClientSecret": "[ClientSecret]",
          "AzureAdGroups": ["The Azure AD groups would like to write back"],
          "AdOrgUnit": "The OU of ADDS for writeback users."
       }
   }
   ```
3. The application is developed using the .NET Core framework so that you can build the project as a Window-x64 Single file Self-contained that can copy and run on any joined domain computer without the .NET runtime required.
4. Run `sc.exe create "AADS Users WriteBack" binpath=[Path To]\Azure.ADDS.UserWriteBack.exe` to install the app as a window service. You need to update the **Logon account** with a window account that **have permission to create ADDS objects** and switch the start type to **Automatic**

> Please note that the log files will be writing to the C:\Windows\System32\Logs folder.

5. The application will check and sync users from **AzureAdGroups** every 1 hour. If you would like to force sync, just restart the service.
6. After the user account writes to the ADDS side. The account owner needs to reset their password as the application will generate a random password with 50 length characters when writing back the user to ADDS.



Thanks

Drunkcoding: https://drunkcoding.net/sharing-azure-ad-to-adds-user-writeback-tools/

Github: https://github.com/baoduy/Azure.ADDS.UserWriteBack
