param(
  [Parameter(Mandatory = $false)]
  [Alias('c')]
  [string]
  $dbContext = 'CgmLinkDbContext',

  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [Alias('n')]
  [string]
  $name
)

$rootDirectory = git rev-parse --show-toplevel

switch ($dbContext) {
    'CgmLinkDbContext' {
        $workingDirectory = $rootDirectory + "/src/CgmLink.Data"
        $projectPath = '../CgmLink.Data.Migrators.MSSQL/'
        $startupProject = '../CgmLink.Api'
    }
    'CgmLinkNutritionDbContext' {
        $workingDirectory = $rootDirectory + "/src/CgmLink.Nutrition.Data"
        $projectPath = '../CgmLink.Nutrition.Data.Migrators.MSSQL/'
        $startupProject = '../CgmLink.Nutrition.Data.Importer'
    }
}

Push-Location $workingDirectory

Write-Host "Adding $dbContext migration $name"

<# MSSQL #>
Write-Host "Adding migrations for MSSQL"

dotnet ef migrations -v add $name --project $projectPath --context $dbContext --startup-project $startupProject --output-dir Migrations

Write-Host -NoNewLine 'Migrations Added. Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');

Pop-Location
