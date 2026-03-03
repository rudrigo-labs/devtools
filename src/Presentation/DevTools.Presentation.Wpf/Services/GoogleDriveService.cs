using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using DevTools.Presentation.Wpf.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace DevTools.Presentation.Wpf.Services
{
    public class GoogleDriveService
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private const string ApplicationName = "DevTools Hub";

        private async Task<DriveService> GetDriveServiceAsync(GoogleDriveSettings settings)
        {
            if (settings == null || !settings.IsEnabled)
            {
                throw new InvalidOperationException("Backup do Google Drive não está habilitado ou configurações estão ausentes.");
            }

            if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ClientSecret))
            {
                throw new InvalidOperationException("Client ID e Client Secret do Google Drive não foram configurados.");
            }

            UserCredential credential;

            // Em vez de ler de um arquivo credentials.json, montamos o objeto em memória
            var clientSecrets = new ClientSecrets
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret
            };

            // Define o local onde o token de acesso será armazenado em AppData/DevTools
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevTools");
            string credPath = Path.Combine(appDataPath, "GoogleDriveToken");

            if (!Directory.Exists(credPath))
            {
                Directory.CreateDirectory(credPath);
            }

            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true));

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private async Task<string> GetOrCreateFolderIdAsync(DriveService service, string folderName)
        {
            var listRequest = service.Files.List();
            listRequest.Q = $"name = '{folderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listRequest.Fields = "files(id, name)";
            
            var result = await listRequest.ExecuteAsync();
            var folder = result.Files.FirstOrDefault();

            if (folder != null)
            {
                return folder.Id;
            }

            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var createRequest = service.Files.Create(folderMetadata);
            createRequest.Fields = "id";
            
            var newFolder = await createRequest.ExecuteAsync();
            return newFolder.Id;
        }

        /// <summary>
        /// Testa a conexão com o Google Drive usando as configurações fornecidas.
        /// </summary>
        public async Task TestConnectionAsync(GoogleDriveSettings settings)
        {
            var service = await GetDriveServiceAsync(settings);
            // Tenta listar arquivos (apenas para validar o serviço)
            var request = service.Files.List();
            request.PageSize = 1;
            await request.ExecuteAsync();
        }

        /// <summary>
        /// Faz o upload do conteúdo das notas para o Google Drive.
        /// Cada salvamento gera um novo arquivo com timestamp para manter o histórico.
        /// </summary>
        public async Task UploadNoteAsync(string content, string fileName, GoogleDriveSettings settings)
        {
            if (settings == null || !settings.IsEnabled) return;

            var service = await GetDriveServiceAsync(settings);
            string folderId = await GetOrCreateFolderIdAsync(service, settings.FolderName ?? "DevToolsNotes");

            // Formata o nome do arquivo com timestamp: anotações.DevToolsNotes_yyyyMMdd_HHmmss.ext
            // O fileName recebido já deve vir limpo (ex: "minha_nota.txt")
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string finalFileName = $"{nameWithoutExt}_{timestamp}{extension}";

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = finalFileName,
                Parents = new List<string> { folderId }
            };

            byte[] byteArray = Encoding.UTF8.GetBytes(content);
            using (var stream = new MemoryStream(byteArray))
            {
                // Sempre cria um novo arquivo (Mirror Backup com histórico)
                var createRequest = service.Files.Create(fileMetadata, stream, GetMimeType(extension));
                await createRequest.UploadAsync();
            }
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".md" => "text/markdown",
                ".txt" => "text/plain",
                _ => "text/plain"
            };
        }
    }
}
