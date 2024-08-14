$name = (Get-Item *.sln).BaseName
$xml = [Xml] (Get-Content .\$name\$name.csproj)
$version = $xml.Project.PropertyGroup.Version
git tag -a v$version -m "Release $version"
git push --tags
