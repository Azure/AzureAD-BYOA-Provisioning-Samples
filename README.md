##Getting Started

Please see the following article for information on sample pre-requisites and getting started:

https://azure.microsoft.com/en-us/documentation/articles/active-directory-scim-provisioning/#building-your-own-provisioning-solution-for-any-application

##AmazonProvisioning Sample Notes

This sample requires libraries from the AWS Toolkit for Visual Studio, which must be downloaded and installed from http://aws.amazon.com/visualstudio/

To resolve the solution references in the AmazonProvisionngAgent solution, in Visual Studio select Tools > Library Package Manager > Package Manager Console, and execute the commands below for each project:

    nuget restore

To resolve the AmazonProvisioningAgentLauncher solution reference to the TemplateWizard library, add a new reference to the following file (path may vary on your system):

    C:\Windows\Microsoft.NET\assembly\GAC_MSIL\AWSToolkit.TemplateWizard\v4.0_2.1.1.0__0b37b66c91780a48\AWSToolkit.TemplateWizard.dll

##FileProvisioning Sample Notes

To resolve the solution references in the FileProvisioningAgent solution, in Visual Studio select Tools > Library Package Manager > Package Manager Console, and execute the command below for the FileProvisioningAgent project:

    nuget restore
