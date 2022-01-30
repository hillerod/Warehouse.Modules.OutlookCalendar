### Install Bygdrift Warehouse module: OutlookCalendar. Read more at: https://github.com/hillerod/Warehouse.Modules.OutlookCalendar

### Variables to be filled out:
$group = "warehouse" # The name on the resource group.
$storageAccount = "warehousestorage" # The name on the storage account. Must be globally unique. Only use lower case. Min 3 letters and max 24
$sqlServerName = "SqlServer"
$sqlServerUser = "user"  # Secret to remember
$sqlServerPass = "ASeriousP1wor!d" # Secret to remember
$sqlDatabaseName = "SqlDbWarehouse"
$functionAppName = "DaluxFMApi"  # The name in the Azure setup (the name of the function app) Must be globally unique
$moduleName = "DaluxFMApi" # The name on the module inside database, datalake
$dataLakeContainer = "ejendomme" # The name on the container in the storage, where you will save data. Could be: ejendomme
$schedule = "0 0 1 * * *"  # How often this module should run. This example runs each night at 1AM UTC. Read more here: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions
$FTPConnectionString = "host=xxx.dk;port=22;user=XXX;pass=XXX;path=/ExchangeBookings;"

# Select actions you want to execute, be setting the following settings to eithe $true or $false:
# Only setup group, storage, sql-server and sql-database one time. Then all new modules can use this environment.
$addNewGroup = $false # Setup the resource group that the whole environment should be created in.
$addNewStorage = $false # Add the data lake
$addNewSqlServer = $false # Add a sql server to host the sql database
$addNewSqlDatabase = $false # Add a sql database
$addModule = $false # Add this module
$updateModule = $false # Only called if there is a new github version that should be imported and updated. The same can be done in the portal, under the current app function > Deployment > Deployment Center > Sync
$callModule = $false # Ths $schedule will call the module at a preset time schedule. With $callModule, you can manually execute the module

### Is AZ installed:
If((-not (Get-Module -ListAvailable -Name "Azure"))){
    Write-Host "The Azure cli is not installed. Read about how to install it here: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli."  -ForegroundColor Red
    cmd /c 'pause'
    exit
}
### Login to Azure
if(-not (az account show --query state --output tsv) -eq "Enabled"){
    az login
}
### Create group:
if ($addNewGroup -eq $true) {
    Write-Host "Setting up resource group..." -ForegroundColor Green
    if((az group exists -g $group) -eq "true"){
        Write-Host "The group name:'"$group"' already exists. The script will stop excecution."  -ForegroundColor Red
        cmd /c 'pause'
        exit    
    }
    az group create -g $group -l westeurope
    Write-Host "Group created." -ForegroundColor Green
}
### Create storage:
if ($addNewStorage -eq $true) {
    Write-Host "Setting up storage..." -ForegroundColor Green
    if((az storage account check-name --name $storageAccount --query nameAvailable) -eq "false"){
        Write-Host "The storage account name:'"$storageAccount"' already exists. The script will stop excecution."  -ForegroundColor Red
        cmd /c 'pause'
        exit
    }
    az storage account create -n $storageaccount -g $group -l westeurope --sku Standard_GRS --kind StorageV2 --enable-hierarchical-namespace true
    Write-Host "Setting up ApplicationInsight..." -ForegroundColor Green
    az config set extension.use_dynamic_install=yes_without_prompt
    az monitor app-insights component create --app appinsights -g $group -l westeurope --kind web --application-type web --retention-time 30
    Write-Host "Storage created." -ForegroundColor Green
}
### Create Sql server:
if ($addNewSqlServer -eq $true) {
    Write-Host "Setting up server..." -ForegroundColor Green
    $sqlserverCreateOutput = (az sql server create -n $sqlServerName -g $group -l westeurope -u $sqlServerUser -p $sqlServerPass) 2>&1
    if($sqlserverCreateOutput -like "Error: Deployment failed.*"){
        $sqlserverCreateOutput
        Write-Host "The script will stop excecution."  -ForegroundColor Red
        cmd /c 'pause'
        exit
    }
    else{
        $sqlserverCreateOutput
        Write-Host "SqlServer created." -ForegroundColor Green
    }
    Write-Host "Setting up firewall..." -ForegroundColor Green
    $myIp = (Invoke-WebRequest -uri "http://ifconfig.me/ip").Content
    az sql server firewall-rule create -s $sqlServerName -g $group -n "MyIp" --start-ip-address $myIp --end-ip-address $myIp #Create a firewll for access for this computer
    az sql server firewall-rule create -s $sqlServerName -g $group -n "AccessForAzureServices" --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 #Create a firewall rule that allows access from Azure services
    Write-Host "Firewall created" -ForegroundColor Green
}
### Create Sql database:
if ($addNewSqlDatabase -eq $true) {
    Write-Host "Setting up database..." -ForegroundColor Green
    $sqlDbCreateOutput = (az sql db create -n $sqlDatabaseName -s $sqlServerName -g $group --service-objective Basic) 2>&1
    if($sqlDbCreateOutput -like "Error: Deployment failed.*"){
        $sqlDbCreateOutput
        Write-Host "The script will stop excecution."  -ForegroundColor Red
        cmd /c 'pause'
        exit
    }
    else{
        $sqlDbCreateOutput
        Write-Host "SqlDb created." -ForegroundColor Green
    }
}
### Add module:
if ($addModule -eq $true) {
    Write-Host "Adding module (can take up to 5-10 minutes)..." -ForegroundColor Green
    $appFuncNameCount = ((az functionapp list -g $group --query '[].name' | ConvertFrom-Json) | Where-Object{$_ -like $functionAppName}).count
    if($appFuncNameCount -gt 0){
        Write-Host "The function name:'"$functionAppName"' already exists. The script will stop excecution."  -ForegroundColor Red
        cmd /c 'pause'
        exit
    }
    $functionAppCreateOutput = (az functionapp create -n $functionAppName -g $group --consumption-plan-location westeurope --storage-account $storageAccount --app-insights appInsights --functions-version 3 --runtime dotnet-isolated --runtime-version 5.0 --deployment-source-url https://github.com/hillerod/Warehouse.Modules.OutlookCalendar) 2>&1
    if($functionAppCreateOutput -like "ERROR: Operation returned an invalid status 'Conflict'"){
        $functionAppCreateOutput
        Write-Host "Perhaps the function app name is not globally unque, so try change it. The script will stop excecution."  -ForegroundColor Red
        cmd /c 'pause'
        exit
    }
    $functionAppCreateOutput  
    $dataLakeConnectionString = az storage account show-connection-string -n $storageAccount -g $group --query "connectionString"
    $databaseConnectionString = (az sql db show-connection-string -n $sqlDatabaseName -s $sqlServerName -c ado.net).Replace('<username>', $sqlServerUser).Replace('<password>', $sqlServerPass)
    az functionapp config appsettings set -n $functionAppName -g $group --settings `
    FTPConnectionString=$FTPConnectionString `
    DatabaseConnectionString=$databaseConnectionString `
    DataLakeConnectionString=$dataLakeConnectionString `
    DataLakeContainer=$dataLakeContainer `
    ModuleName=$moduleName `
    Schedule=$schedule
    Write-Host "Module created." -ForegroundColor Green
}
### Update module:
if ($updateModule -eq $true) {
    Write-Host "Updating module..." -ForegroundColor Green
    if((az functionapp show -n $functionAppName -g $group --query enabled) -eq "false"){
        Write-Host "The function name:'"$functionAppName"' doesn't exist. The script will stop excecution."  -ForegroundColor Red
        cmd /c 'pause'
        exit
    }
    az functionapp deployment source sync -n $functionAppName -g $group
    Write-Host "Module update has been started. Within 5 minutes, the new version should be running." -ForegroundColor Green
}
### Call module:
if ($callModule -eq $true) {
    Write-Host "Calling module..." -ForegroundColor Green
    $functionKey=$(az functionapp keys list -n $functionAppName -g $group --query "masterKey" --output tsv)
    $functionUrl=$(az functionapp function show -n $functionAppName -g $group --function-name TimerTrigger --query href --output tsv)
    $body = '{ "input": "test" }'
    $header = @{
    "x-functions-key" = $functionKey
    "Content-Type" = "application/json"
    }
    Invoke-RestMethod -Uri $functionUrl -Method 'Post' -Body $body -Headers $header | ConvertTo-HTML
    Write-Host "Module was called." -ForegroundColor Green
}

Write-Host "The script has reached the end." -ForegroundColor Green
cmd /c 'pause'
exit