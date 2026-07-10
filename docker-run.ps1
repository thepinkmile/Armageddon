param(
	[string] $ImageName = 'armageddon-tests',
	[string] $ContainerName = 'armageddon-tests-run'
)

Write-Host "Building Docker image $ImageName..."
docker build -t $ImageName .

Write-Host "Running tests inside container..."
docker run --rm -v ${PWD}:/workspace -v ${PWD}/tests/Armageddon.Tests/TestResults:/workspace/tests/Armageddon.Tests/TestResults --name $ContainerName $ImageName

Write-Host "Also building and running API and Web containers for local development preview..."
docker build -f src/Armageddon.Api/Dockerfile -t armageddon-api ./
docker build -f src/Armageddon.Web/Dockerfile -t armageddon-web ./
Write-Host "Built armageddon-api and armageddon-web images. Use 'docker run -p 5047:80 armageddon-api' and 'docker run -p 5281:80 armageddon-web' to run them locally."

Write-Host "Finished. Coverage and test results are in ./tests/Armageddon.Tests/TestResults on the host."
