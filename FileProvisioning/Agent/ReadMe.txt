This project provides the code for a sample application illustrating 
how any Microsoft.SystemForCrossDomainIdentityManagement.IProvider 
can be hosted within a Microsoft.Graph.Provisioning.Agent.  

Microsoft.Graph.Provisioning.IAgent.ProvisionAsync(), the agent will 
connect out to Azure Active Directory and proceed to synchronize changes in a 
designated Azure Active Directory down to the hosted 
Microsoft.SystemForCrossDomainIdentityManagement.IProvider.  

The sample executable build from the code in this project can be executed in this way: 
    FileAgnt <the name of the .csv file to which the hosted file IProvider is to synchronize data> <the globally-unique identifier of an Azure Active Directory tenant> <any globally-unique identifier for the synchronization task> <the user name of the administrator of the Azure Active Directory> <the password of the administrator of the Azure Active Directory>

If multiple instances of a Microsoft.Graph.Provisioning.Agent are executed, 
then Azure Active Directory will transparently designate one of them as the active instance.  
Should that instance disconnect, then Azure Active Directory will transparently fail over to 
another instance within a very few minutes.  

