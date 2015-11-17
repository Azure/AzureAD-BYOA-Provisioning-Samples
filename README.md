##Getting Started

Please see the following article for information on sample pre-requisites and getting started:

https://azure.microsoft.com/en-us/documentation/articles/active-directory-scim-provisioning/#building-your-own-provisioning-solution-for-any-application

##AmazonProvisioning Sample Notes

This sample requires libraries from the AWS Toolkit for Visual Studio, which must be downloaded and installed from http://docs.aws.amazon.com/AWSToolkitVS/latest/UserGuide/tkv_setup.html

To resolve the solution references to the AWSSDK library, add a reference to the following file after installation (path may vary on your system):

    C:\Program Files (x86)\AWS SDK for .NET\past-releases\Version-2\AWSSDK.dll
    
To resolve the AmazonProvisioningAgentLauncher solution reference to the TemplateWizard library, add a reference to the following file (path may vary on your system):

    C:\Windows\Microsoft.NET\assembly\GAC_MSIL\AWSToolkit.TemplateWizard\v4.0_2.1.1.0__0b37b66c91780a48\AWSToolkit.TemplateWizard.dll

To resolve the other solution references, select Tools > Library Package Manager > Package Manager Console, and execute the commands below for the FileProvisioningAgent project 

    Install-Package Microsoft.SystemForCrossDomainIdentityManagement
    Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory
    Install-Package Microsoft.Owin.Diagnostics
    Install-Package Microsoft.Owin.Host.SystemWeb

##FileProvisioning Sample Notes

To resolve the solution references, select Tools > Library Package Manager > Package Manager Console, and execute the commands below for the FileProvisioningAgent project 

    Install-Package Microsoft.SystemForCrossDomainIdentityManagement
    Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory
    Install-Package Microsoft.Owin.Diagnostics
    Install-Package Microsoft.Owin.Host.SystemWeb
