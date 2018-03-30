# Microsoft Azure Active Directory ![](http://www.simplecloud.info/img/logo/SCIM_B-and-W_72x24.png) Guidelines 

## General Guidelines
* Microsoft recommends following the [SCIM 2.0 standard](http://www.simplecloud.info/#Specification).
* `id` is a required property for all the resources; except for `ListResponse` with zero members.
* Response to a query/filter request should always be a `ListResponse`.
* Groups are only supported if the SCIM implementation supports PATCH requests.
* It is not necessary to include the entire resource in the PATCH response.
* Microsoft Azure AD only uses the following operators  
     - `eq`
     - `and`
* Microsoft Azure AD makes requests to fetch a random user and group to ensure that the endpoint and the credentials are valid. It is also done as a part of **Test Connection** flow in the [Azure portal](https://portal.azure.com). 
* The attribute that the resources can be queried on should be set as a matching attribute on the application in the [Azure portal](https://portal.azure.com).<br> 
Reference : [Customizing User Provisioning Attribute Mappings](https://docs.microsoft.com/en-us/azure/active-directory/active-directory-saas-customizing-attribute-mappings)

## User Operations

* Users can be queried by `userName` or `email[type eq "work"]` attributes. For queries using another attribute, please contact [anchheda@microsoft.com](mailto:anchheda@microsoft.com). 

### Create User

###### Request
*POST /Users*
```json
{
	"schemas": [
	    "urn:ietf:params:scim:schemas:core:2.0:User",
	    "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"],
	"externalId": "0a21f0f2-8d2a-4f8e-bf98-7363c4aed4ef",
	"userName": "Test_User_ab6490ee-1e48-479e-a20b-2d77186b5dd1",
	"active": true,
	"emails": [{
		"primary": true,
		"type": "work",
		"value": "Test_User_fd0ea19b-0777-472c-9f96-4f70d2226f2e@testuser.com"
	}],
	"meta": {
		"resourceType": "User"
	},
	"name": {
		"formatted": "givenName familyName",
		"familyName": "familyName",
		"givenName": "givenName"
	},
	"roles": []
}
```

##### Response
*HTTP/1.1 201 Created*
```json
{
	"schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
	"id": "48af03ac28ad4fb88478",
	"externalId": "0a21f0f2-8d2a-4f8e-bf98-7363c4aed4ef",
	"meta": {
		"resourceType": "User",
		"created": 1522180232479,
		"lastModified": 1522180232481,
	},
	"userName": "Test_User_ab6490ee-1e48-479e-a20b-2d77186b5dd1",
	"name": {
		"formatted": "givenName familyName",
		"familyName": "familyName",
		"givenName": "givenName",
	},
	"active": true,
	"emails": [{
		"value": "Test_User_fd0ea19b-0777-472c-9f96-4f70d2226f2e@testuser.com",
		"type": "work",
		"primary": true
	}]
}
```


### Get User

###### Request
*GET /Users/5d48a0a8e9f04aa38008* 

###### Response
*HTTP/1.1 200 OK*
```json
{
	"schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
	"id": "5d48a0a8e9f04aa38008",
	"externalId": "58342554-38d6-4ec8-948c-50044d0a33fd",
	"meta": {
		"resourceType": "User",
		"created": 1522180660000,
		"lastModified": 1522180660000,
	},
	"userName": "Test_User_feed3ace-693c-4e5a-82e2-694be1b39934",
	"name": {
		"formatted": "givenName familyName",
		"familyName": "familyName",
		"givenName": "givenName",
	},
	"active": true,
	"emails": [{
		"value": "Test_User_22370c1a-9012-42b2-bf64-86099c2a1c22@testuser.com",
		"type": "work",
		"primary": true
	}]
}
```
### Get User by query

##### Request
*GET /Users?filter=userName eq "Test_User_dfeef4c5-5681-4387-b016-bdf221e82081"*

##### Response
*HTTP/1.1 200 OK*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
	"totalResults": 1,
	"Resources": [{
		"schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
		"id": "2441309d85324e7793ae",
		"externalId": "7fce0092-d52e-4f76-b727-3955bd72c939",
		"meta": {
			"resourceType": "User",
			"created": "2018-03-27T19:59:26.000Z",
			"lastModified": "2018-03-27T19:59:26.000Z",
			
		},
		"userName": "Test_User_dfeef4c5-5681-4387-b016-bdf221e82081",
		"name": {
			"familyName": "familyName",
			"givenName": "givenName"
		},
		"active": true,
		"emails": [{
			"value": "Test_User_91b67701-697b-46de-b864-bd0bbe4f99c1@testuser.com",
			"type": "work",
			"primary": true
		}],
		"groups": []
	}],
	"startIndex": 1,
	"itemsPerPage": 20
}

```

### Get User by query - Zero results

##### Request
*GET /Users?filter=userName eq "non-existent user"*

##### Response
*HTTP/1.1 200 OK*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
	"totalResults": 0,
	"Resources": [],
	"startIndex": 1,
	"itemsPerPage": 20
}

```

### Update User [Multi-valued properties]

##### Request
*PATCH /Users/6764549bef60420686bc HTTP/1.1*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
	"Operations": [
            {
    		"op": "Replace",
    		"path": "emails[type eq \"work\"].value",
    		"value": "updatedEmail@microsoft.com"
    	    },
    	    {
    		"op": "Replace",
    		"path": "name.familyName",
    		"value": "updatedFamilyName"
    	    }
	]
}
```

##### Response
*HTTP/1.1 200 OK*
```json
{
	"schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
	"id": "6764549bef60420686bc",
	"externalId": "6c75de36-30fa-4d2d-a196-6bdcdb6b6539",
	"meta": {
		"resourceType": "User",
		"created": 1522180894000,
		"lastModified": 1522180894000
	},
	"userName": "Test_User_fbb9dda4-fcde-4f98-a68b-6c5599e17c27",
	"name": {
		"formatted": "givenName updatedFamilyName",
		"familyName": "updatedFamilyName",
		"givenName": "givenName"
	},
	"active": true,
	"emails": [{
		"value": "updatedEmail@microsoft.com",
		"type": "work",
		"primary": true
	}]
}
```

### Update User [Single-valued properties]

##### Request
*PATCH /Users/5171a35d82074e068ce2 HTTP/1.1*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
	"Operations": [{
		"op": "Replace",
		"path": "userName",
		"value": "5b50642d-79fc-4410-9e90-4c077cdd1a59@testuser.com"
	}]
}
```

##### Response
*HTTP/1.1 200 OK*
```json
{
	"schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
	"id": "5171a35d82074e068ce2",
	"externalId": "aa1eca08-7179-4eeb-a0be-a519f7e5cd1a",
	"meta": {
		"resourceType": "User",
		"created": 1522181044000,
		"lastModified": 1522181044000,
		
	},
	"userName": "5b50642d-79fc-4410-9e90-4c077cdd1a59@testuser.com",
	"name": {
		"formatted": "givenName familyName",
		"familyName": "familyName",
		"givenName": "givenName",
	},
	"active": true,
	"emails": [{
		"value": "Test_User_49dc1090-aada-4657-8434-4995c25a00f7@testuser.com",
		"type": "work",
		"primary": true
	}]
}
```

### Delete User

##### Request
*DELETE /User/5171a35d82074e068ce2 HTTP/1.1*

##### Response
*HTTP/1.1 204 No Content*

## Group Operations

* Groups shall always be created with an empty members list.
* Groups can be queried by the `displayName` attribute.
* Update to the group PATCH request should yield an *HTTP 204 No Content* in the response. Returning a body with a list of all the members is not advisable.
* It is not necessary to support returning all the members of the group.

### Create Group

##### Request
*POST /Groups HTTP/1.1*
```json
{
	"schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group", "http://schemas.microsoft.com/2006/11/ResourceManagement/ADSCIM/2.0/Group"],
	"externalId": "8aa1a0c0-c4c3-4bc0-b4a5-2ef676900159",
	"id": "c4d56c3c-bf3b-4e96-9b64-837018d6060e",
	"displayName": "displayName",
	"members": [],
	"meta": {
		"resourceType": "Group"
	}
}
```

##### Response
*HTTP/1.1 201 Created*
```json
{
	"schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
	"id": "927fa2c08dcb4a7fae9e",
	"externalId": "8aa1a0c0-c4c3-4bc0-b4a5-2ef676900159",
	"meta": {
		"resourceType": "Group",
		"created": 1522188145230,
		"lastModified": 1522188145230,
		
	},
	"displayName": "displayName",
	"members": []
}
```

### Get Group

##### Request
*GET /Groups/40734ae655284ad3abcc?excludedAttributes=members HTTP/1.1*

##### Response
*HTTP/1.1 200 OK*
```json
{
	"schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
	"id": "40734ae655284ad3abcc",
	"externalId": "60f1bb27-2e1e-402d-bcc4-ec999564a194",
	"meta": {
		"resourceType": "Group",
		"created": 1522188140000,
		"lastModified": 1522188140000
	},
	"displayName": "displayName",
}
```

### Get Group by displayName

##### Request
*GET /Groups?excludedAttributes=members&filter=displayName eq "displayName" HTTP/1.1*

##### Response
*HTTP/1.1 200 OK*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
	"totalResults": 1,
	"Resources": [{
		"schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
		"id": "8c601452cc934a9ebef9",
		"externalId": "0db508eb-91e2-46e4-809c-30dcbda0c685",
		"meta": {
			"resourceType": "Group",
			"created": "2018-03-27T22:02:32.000Z",
			"lastModified": "2018-03-27T22:02:32.000Z",
			
		},
		"displayName": "displayName",
	}],
	"startIndex": 1,
	"itemsPerPage": 20
}
```
### Update Group [Non-member attributes]

##### Request
*PATCH /Groups/fa2ce26709934589afc5 HTTP/1.1*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
	"Operations": [{
		"op": "Replace",
		"path": "displayName",
		"value": "1879db59-3bdf-4490-ad68-ab880a269474updatedDisplayName"
	}]
}
```

##### Response
*HTTP/1.1 204 No Content*

### Update Group [Add Members]

##### Request
*PATCH /Groups/a99962b9f99d4c4fac67 HTTP/1.1*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
	"Operations": [{
		"op": "Add",
		"path": "members",
		"value": [{
			"$ref": null,
			"value": "f648f8d5ea4e4cd38e9c"
		}]
	}]
}
```

##### Response
*HTTP/1.1 204 No Content*

### Update Group [Remove Members]

##### Request
*PATCH /Groups/a99962b9f99d4c4fac67 HTTP/1.1*
```json
{
	"schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
	"Operations": [{
		"op": "Remove",
		"path": "members",
		"value": [{
			"$ref": null,
			"value": "f648f8d5ea4e4cd38e9c"
		}]
	}]
}
```

##### Response
*HTTP/1.1 204 No Content*

### Delete Group

##### Request
*DELETE /Groups/cdb1ce18f65944079d37 HTTP/1.1*

##### Response
*HTTP/1.1 204 No Content*

