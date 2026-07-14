param (
    $baseUrl = "https://api.mattgerega.com/unifi/ipmanager"    
)

$outFolder = ".\openapiclient"
& npx @openapitools/openapi-generator-cli generate -g typescript-axios -o "$outFolder" -i "$baseUrl/swagger/unifi.ipmanager/swagger.json"

Write-Host "Copying files to .\src\api"
Copy-Item "$outfolder\api.ts" ".\src\api\"
Copy-Item "$outfolder\base.ts" ".\src\api\"
Copy-Item "$outfolder\configuration.ts" ".\src\api\"
Copy-Item "$outfolder\common.ts" ".\src\api\"
Copy-Item "$outfolder\index.ts" ".\src\api\"

#Write-Host "Running ESLint on newly generated files..."
#& npx eslint --ignore-path .eslintignore --fix --ext .tsx --ext .ts --ext .js --ext .jsx .\ClientApp\src\api\*

#Write-Host "Running `prettier` on newly generated files..."
#npx prettier --write .\ClientApp\src\api\*
