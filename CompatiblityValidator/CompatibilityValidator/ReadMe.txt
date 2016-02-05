This project provides the code for a console application that can be used to confirm that 
a specified System for Cross-Domain Identity Management endpoint can be integrated with 
Azure Active Directory.  To output a report to the console, build the solution and then, 
from a console, execute the application as follows: 
    
    SCIMCV <a domain name, such as "contoso.com"> <the base address of the service, such as "https://localhost:9000"> <an OAuth bearer token> <1.0|1.1|2.0> [true if group compatibility is to be tested, otherwise false] [the name of the user filtering attribute, which, by default, is externalId] [the name of the group filtering attribute, which, by default, is externalId]