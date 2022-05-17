@minLength(3)
@maxLength(24)
@description('The name of the module. Only use between 3-24 letters or numers, or the Warehouse can\'t function. The functionApp gets the same name followed by the "-resourceGroup.id". The modulename is hard to change later, so try to keep it static. It is used in dataLake and databse as an identifier of data that comes from this app')
param moduleName string = 'OutlookCalendar'

@secure()
param fTPConnectionString string = ''


@description('"Romance Standard Time" is Copenhagen. For other timezones, find them here: https://raw.githubusercontent.com/Bygdrift/Warehouse/master/Docs/TimeZoneIds.csv')
param timeZoneId string = 'Romance Standard Time'

var appStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${appStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(appStorage.id, appStorage.apiVersion).keys[0].value}'
var functionAppName = '${moduleName}-${uniqueString(resourceGroup().id)}'

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
  name: 'keyvault-${uniqueString(resourceGroup().id)}'
  scope: resourceGroup()
}

resource FTPConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: 'Secret--${moduleName}--FTPConnectionString'
  parent: keyVault
  properties: {
    value: fTPConnectionString
  }
}

resource appStorage 'Microsoft.Storage/storageAccounts@2019-06-01' existing = {
  name: 'appstorage${uniqueString(resourceGroup().id)}'
}

resource dataLake 'Microsoft.Storage/storageAccounts@2019-06-01' existing = {
  name: 'datalake${uniqueString(resourceGroup().id)}'
}

resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' existing = {
  name: 'applicationInsights'
}

resource windowsHostingPlan 'Microsoft.Web/serverfarms@2020-10-01' = {
  name: 'windows-${uniqueString(resourceGroup().id)}'
  location: resourceGroup().location
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2021-02-01' = {
  kind: 'functionapp'
  name: functionAppName
  location: resourceGroup().location
  identity:{
    type: 'SystemAssigned'  //Key vault access: https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet
  }
  properties: {
    serverFarmId: windowsHostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'AzureWebJobsStorage'
          value: appStorageConnectionString
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'HostName'
          value: '${functionAppName}.azurewebsites.net'
        }
        {
          name: 'ModuleName'
          value: moduleName
        }
        {
          name: 'TimeZoneId'
          value: timeZoneId
        }
        {
          name: 'VaultUri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: appStorageConnectionString
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: null
        }
        // name: 'WEBSITE_CONTENTSHARE' // will also be auto-generated - https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings#website_contentshare
        // WEBSITE_RUN_FROM_PACKAGE will be set to 1 by func azure functionapp publish
      ]
    }
  }
}

resource githubSource 'Microsoft.Web/sites/sourcecontrols@2021-01-01' = {
  name: '${functionApp.name}/web'
  properties: {
    repoUrl: 'https://github.com/hillerod/Warehouse.Modules.OutlookCalendar'
    branch: 'master'
    isManualIntegration: true
  }
}

resource keyVaultPolicies 'Microsoft.KeyVault/vaults/accessPolicies@2021-06-01-preview' = {
  dependsOn:[
    keyVault
  ]
  name: '${keyVault.name}/add'
  properties: {    
    accessPolicies: [
      {
        objectId: functionApp.identity.principalId
        permissions: {
          secrets: [ 
            'get'
            'list'
            'set'
            'delete'
          ]
        }
        tenantId: functionApp.identity.tenantId //  subscription().tenantId
      }
    ]
  }
}  

output functionAppName string = functionAppName
