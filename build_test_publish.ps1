git clean -fxd
dotnet build src/PDFiumCoreBindingsGenerator/PDFiumCoreBindingsGenerator.csproj -c Release
dotnet ./src/PDFiumCoreBindingsGenerator/bin/Release/net6.0/PDFiumCoreBindingsGenerator.dll latest true
dotnet pack ./src/PDFiumCore/PDFiumCore.csproj -c Release -o ./artifacts/
dotnet test ./src/PDFiumCore.Tests/PDFiumCore.Tests.csproj
if($?)
{
    $xmlNode = Select-Xml -Path src/Directory.Build.props -XPath '/Project/PropertyGroup/Version' 
    $version = "v" + ($xmlNode.Node.InnerXML)
	$commitMessage = "PDFium version " + $version
	git commit -a -m $commitMessage
    git tag $version
	
	$confirmation = Read-Host "Do you want to push the new tag? (y/n)"
	if ($confirmation -eq 'y') {
		git push origin
		git push origin $version 
	}
}
else 
{
	echo "Tests Failed."
}
pause