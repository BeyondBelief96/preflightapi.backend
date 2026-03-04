@description('Azure region for log query alert rules')
param location string

@description('Email address for alert notifications')
param alertEmail string

@description('Resource ID of the API Web App (for HTTP 5xx metric alert)')
param webAppId string

@description('Resource ID of the Function App Application Insights (for log query alerts)')
param functionAppInsightsId string

// ─── Action Group ──────────────────────────────────────────────────────────────

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-preflight-alerts'
  location: 'global'
  properties: {
    groupShortName: 'PflightAlert'
    enabled: true
    emailReceivers: [
      {
        name: 'admin-email'
        emailAddress: alertEmail
      }
    ]
  }
}

// ─── API: HTTP 5xx Spike ───────────────────────────────────────────────────────

resource api5xxAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'alert-api-5xx-spike'
  location: 'global'
  properties: {
    description: 'Fires when the API returns more than 5 HTTP 5xx errors in a 5-minute window'
    severity: 1
    enabled: true
    scopes: [webAppId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'Http5xxSpike'
          metricName: 'Http5xx'
          metricNamespace: 'Microsoft.Web/sites'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// ─── Functions: Execution Failures ─────────────────────────────────────────────

resource funcFailuresAlert 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = {
  name: 'alert-func-failures'
  location: location
  properties: {
    displayName: 'Function Execution Failures'
    description: 'Fires when Azure Functions have more than 3 failed executions in 10 minutes'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    scopes: [functionAppInsightsId]
    criteria: {
      allOf: [
        {
          query: 'requests | where success == false'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 3
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// ─── Functions: Schema Drift — Breaking (Missing Required Fields) ──────────────

resource schemaDriftBreakingAlert 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = {
  name: 'alert-schema-drift-breaking'
  location: location
  properties: {
    displayName: 'Schema Drift - Breaking (Missing Fields)'
    description: 'Fires when FAA/NOAA APIs remove or rename required fields that the application depends on. Needs immediate investigation.'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT15M'
    windowSize: 'PT15M'
    scopes: [functionAppInsightsId]
    criteria: {
      allOf: [
        {
          query: 'traces | where severityLevel == 3 | where message has "Schema drift detected"'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// ─── Functions: Schema Drift — New Fields Detected ─────────────────────────────

resource schemaDriftNewFieldsAlert 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = {
  name: 'alert-schema-drift-new-fields'
  location: location
  properties: {
    displayName: 'Schema Drift - New Fields Detected'
    description: 'Fires when FAA/NOAA APIs add new fields not present in schema manifests. Informational - update manifests when convenient.'
    severity: 3
    enabled: true
    evaluationFrequency: 'PT15M'
    windowSize: 'PT15M'
    scopes: [functionAppInsightsId]
    criteria: {
      allOf: [
        {
          query: 'traces | where severityLevel == 2 | where message has "Schema drift detected"'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}
