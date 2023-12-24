function Push-Junctions ($Source, $Destination, $VersionToApply = "1.4")
{
    $aboutSrc = Join-Path $Source 'About'
    $defsSrc = Join-Path $Source 'Defs'
    $languagesSrc = Join-Path $Source 'Languages'
    $prepareProcedurallyBinDebug = Join-Path $Source 'src\Necrofancy.PrepareProcedurally\bin\Debug'
    $prepareProcedurallyBinRelease = Join-Path $Source 'src\Necrofancy.PrepareProcedurally\bin\Release'

    $prepareProcedurallyFolder = Join-Path $Destination "Necrofancy.PrepareProcedurally"
    $prepareProcedurallyAbout = Join-Path $Destination 'Necrofancy.PrepareProcedurally\About'
    $prepareProcedurallyDefs = Join-Path $Destination 'Necrofancy.PrepareProcedurally\Defs'
    $prepareProcedurallyLanguages = Join-Path $Destination 'Necrofancy.PrepareProcedurally\Languages'
    $prepareProcedurallyAssemblies = Join-Path $Destination "Necrofancy.PrepareProcedurally\$VersionToApply\Assemblies"

    New-Item -ItemType Directory -Path $prepareProcedurallyFolder -Force
    New-Item -ItemType Junction -Path $prepareProcedurallyAbout -Target $aboutSrc -Force
    New-Item -ItemType Junction -Path $prepareProcedurallyDefs -Target $defsSrc -Force
    New-Item -ItemType Junction -Path $prepareProcedurallyLanguages -Target $languagesSrc -Force

    New-Item -ItemType Directory -Path $prepareProcedurallyAssemblies -Force

    Remove-Item $prepareProcedurallyBinDebug -Recurse
    New-Item -ItemType Junction -Path $prepareProcedurallyBinDebug -Target $prepareProcedurallyAssemblies
    
    Remove-Item $prepareProcedurallyBinRelease -Recurse
    New-Item -ItemType Junction -Path $prepareProcedurallyBinRelease -Target $prepareProcedurallyAssemblies

    $prepareProcedurallyTestModFolder = Join-Path $Destination "Necrofancy.PrepareProcedurally.Test.Mod"
    $testModAboutSrc = Join-Path $Source 'src\Necrofancy.PrepareProcedurally.Test.Mod\About'

    $testModBinDebug = Join-Path $Source 'src\Necrofancy.PrepareProcedurally.Test.Mod\bin\Debug'
    $testModBinRelease = Join-Path $Source 'src\Necrofancy.PrepareProcedurally.Test.Mod\bin\Release'
    $testModAssemblies = Join-Path $Destination "Necrofancy.PrepareProcedurally.Test.Mod\$VersionToApply\Assemblies"
    $testModAbout = Join-Path $Destination 'Necrofancy.PrepareProcedurally.Test.Mod\About'
    
    New-Item -ItemType Directory -Path $prepareProcedurallyTestModFolder -Force
    New-Item -ItemType Junction -Path $testModAbout -Target $testModAboutSrc -Force
    New-Item -ItemType Directory -Path $testModAssemblies -Force

    Remove-Item $testModBinDebug -Recurse
    New-Item -ItemType Junction -Path $testModBinDebug -Target $testModAssemblies -Force

    Remove-Item $testModBinRelease -Recurse
    New-Item -ItemType Junction -Path $testModBinRelease -Target $testModAssemblies -Force
}