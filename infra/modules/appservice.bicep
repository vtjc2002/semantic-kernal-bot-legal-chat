param location string
param appServicePlanName string
param appServiceName string
param msiID string
param msiClientID string
param sku string = 'S1'
param tags object = {}
param openaiGPTModel string
param openaiEmbeddingsModel string

param openaiName string
param storageName string
param searchName string
var searchNames = !empty(searchName) ? [searchName] : []

param openaiEndpoint string
param searchEndpoint string
param cosmosEndpoint string


resource openai 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: openaiName
}


resource storage 'Microsoft.Storage/storageAccounts@2021-09-01' existing = {
  name: storageName
}

resource searchAccounts 'Microsoft.Search/searchServices@2023-11-01' existing = [for name in searchNames: {
  name: name
}]


resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: sku
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  tags: union(tags, { 'azd-service-name': 'semantic-kernel-bot-app' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${msiID}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      http20Enabled: true
      appSettings: [
        {
          name: 'MicrosoftAppType'
          value: 'UserAssignedMSI'
        }
        {
          name: 'MicrosoftAppId'
          value: msiClientID
        }
        {
          name: 'MicrosoftAppTenantId'
          value: tenant().tenantId
        }
        {
          name: 'AOAI_API_ENDPOINT'
          value: openaiEndpoint
        }
        {
          name: 'AOAI_API_KEY'
          value: openai.listKeys().key1
        }
        {
          name: 'AOAI_GPT_MODEL'
          value: openaiGPTModel
        }
        {
          name: 'AOAI_EMBEDDINGS_MODEL'
          value: openaiEmbeddingsModel
        }
        {
          name: 'SEARCH_API_ENDPOINT'
          value: searchEndpoint
        }
        {
          name: 'SEARCH_API_KEY'
          value: !empty(searchNames) ? searchAccounts[0].listQueryKeys().value[0].key : ''
        }
        {
          name: 'SEARCH_INDEX'
          value: 'index-name'
        }
        {
          name: 'SEARCH_SEMANTIC_CONFIG'
          value: 'index-name-semantic-configuration'
        }
        {
          name: 'COSMOS_API_ENDPOINT'
          value: cosmosEndpoint
        }
        {
          name: 'DIRECT_LINE_SECRET'
          value: ''
        }
        {
          name: 'PROMPT_WELCOME_MESSAGE'
          value: 'Welcome to Legal virtual assisant. Ask me questions about a particular agreement. I can help with stock,securites,asset purchase agreements.  Keep in mind that I am power by AI, mistakes can happen.'
        }
        {
          name: 'PROMPT_SYSTEM_MESSAGE'
          value: 'You are legal document assistant that helps find answer in stock purchase agreement / Securites Purchase Agreement / Asset Purchase Agreement.  These legal files are complex in nature so use your knowledge in legal to answer the user's questions.'
        }
        {
          name: 'PROMPT_SYSTEM_MESSAGE_2'
          value: 'Answer the questions as accurately as possible using the provided functions. Only use one function at a time.'
        }
        {
          name: 'PROMPT_SYSTEM_MESSAGE_3'
          value: '[IMPORTANT] You will need file name to help answer the questions. And only answer questions from provided functions, anything else please say "I cannot help you with that".'
        }
        {
          name: 'PROMPT_SYSTEM_MESSAGE_4'
          value: 'If [ENTIRE PDF CONTENT] is in the history, do not call other functions, soley use the history to answer the questions.'
        }
        {
          name: 'PROMPT_SUGGESTED_QUESTIONS'
          value: '[\\"Who is the seller for Depomed agreement?\\",\\"What is the purchase amount in dollars for first choice health agreement?\\",\\"Search the entire 3d systems corp agreement.\\"]'
        }
        {
          name: 'SSO_ENABLED'
          value: 'false'
        }
        {
          name: 'SSO_CONFIG_NAME'
          value: ''
        }
        {
          name: 'SSO_MESSAGE_TITLE'
          value: 'Please sign in to continue.'
        }
        {
          name: 'SSO_MESSAGE_PROMPT'
          value: 'Sign in'
        }
        {
          name: 'SSO_MESSAGE_SUCCESS'
          value: 'User logged in successfully! Please repeat your question.'
        }
        {
          name: 'SSO_MESSAGE_FAILED'
          value: 'Log in failed. Type anything to retry.'
        }


      ]
    }
  }
}

output appName string = appService.name
output hostName string = appService.properties.defaultHostName
