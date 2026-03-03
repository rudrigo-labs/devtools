
$files = Get-ChildItem -Recurse -File | Select-String -Pattern "<<<<<<<" -List | Select-Object -ExpandProperty Path

foreach ($file in $files) {
    Write-Host "Processing $file"
    $content = Get-Content $file -Raw
    
    # Regex to capture the content between ======= and >>>>>>> features/melhorias-by-main
    # We use Singleline mode (?s) so . matches newlines
    # We look for the pattern and replace it with group 1
    
    $newContent = [Regex]::Replace($content, "(?s)(.*?)", '$1')
    
    # Check if there are still any markers left (e.g. if the branch name was different or HEAD was different)
    if ($newContent -match "<<<<<<<") {
        Write-Warning "File $file still has conflict markers after replacement. Please check manually."
    }
    
    Set-Content -Path $file -Value $newContent -NoNewline
}
