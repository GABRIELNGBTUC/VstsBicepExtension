# Arrêter le script en cas d'erreur
$ErrorActionPreference = "Stop"

# Déterminer le répertoire racine
$root = Join-Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent) ".."

# Exécuter la commande dotnet run avec les arguments appropriés
dotnet run --project "$root/" -- --outdir "$root/types"