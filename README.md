[![Build Status](https://ankitchheda.visualstudio.com/SCIMSamples/_apis/build/status/ankitC.AzureAD-BYOA-Provisioning-Samples)](https://ankitchheda.visualstudio.com/SCIMSamples/_build/latest?definitionId=1)

## Getting Started

Please see the following article for information on sample prerequisites and getting started:

https://azure.microsoft.com/en-us/documentation/articles/active-directory-scim-provisioning/#building-your-own-provisioning-solution-for-any-application

The software development kit referred to by the samples assume that .NET 4.5.1 is installed.  That platform can be downloaded from https://www.microsoft.com/en-us/download/details.aspx?id=40773.  

The samples use a simple System for Cross-Domain Identity Manager provider implementation, of which the source code is provided.  That provider synchronizes data with a file of comma-separated values, and it uses version 12.0 of the Microsoft Office Access Connectivity Engine to traverse the file.  To install that engine, proceed to https://www.microsoft.com/en-us/download/details.aspx?id=13255 to download and install the 64-bit version of Microsoft.ACE.OLEDB.12.0.dll.  

After opening any of the Visual Studio 2015 solution files, the .sln files, restore the references to NuGet package libraries by following these steps: 

1.  Open the Visual Studio Package Manager Console by choosing Tools|NuGet Package Manager|Package Manager Console from the Visual Studio menus.  
2.  From the Package Source drop-down list o fhte Package Manager Console, select nuget.org.  
3.  At the Package Manager prompt, execute this command: 
      Update-Package -Reinstall

