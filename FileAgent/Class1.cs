namespace FileAgent 
{
    public class FileHandle
    {
        public string fileName = "";
        public string wrappingDirectory = "";
        public string localPath = "";
        public string fullFilePath = "";
        public FileItemStatus fileState = FileItemStatus.NoInfo;
        static internal List<FileHandle> createdFiles = new List<FileHandle> ();

        public enum FileItemStatus {
            NoInfo, // Info has yet to be gathered
            Exists, // A File with the same name existed
            Denied, // Failed to create the file
            Missing, // File isn't present during check
            Created, // successfully created the file
            Retracted // A file was to be created, but prevented due to unmet requirements 
        }

        static public FileHandle EstablishNestedFileConnection(string fileName, string wrapinDirectory, string? path = null, bool insist = true) {
            FileHandle result = new FileHandle() {
                fileName = fileName,
                wrappingDirectory = wrapinDirectory,
                fileState = FileItemStatus.NoInfo
            };

            if (path != null) {
                (FileItemStatus directoryStatus, FileItemStatus fileStatus) status = TryCreateNestedFile(path, wrapinDirectory, fileName);
                result.fileState = status.fileStatus;
                result.localPath = path;
                result.fullFilePath = Path.Combine(path, wrapinDirectory, fileName);
                if (status.directoryStatus == FileItemStatus.Created) createdFiles.Add(result);
                if (result.fileState == FileItemStatus.Exists || result.fileState == FileItemStatus.Created) return result;
            }
            if (!insist) return result;

            while (result.fileState != FileItemStatus.Exists || result.fileState != FileItemStatus.Created) { 
                (FileItemStatus directoryStatus, FileItemStatus fileStatus) status = TryCreateNestedFile(result.localPath, wrapinDirectory, fileName);
                result.fullFilePath = Path.Combine(result.localPath, wrapinDirectory, fileName);
                result.fileState = status.fileStatus;
                if (status.directoryStatus == FileItemStatus.Created) createdFiles.Add(result);
                if (result.fileState == FileItemStatus.Exists || result.fileState == FileItemStatus.Created) return result;
            }

            return result;
        }
        
        static public FileHandle EstablishFileConnection(string fileName, string? path = null, bool insist = false) {
            FileHandle result = new FileHandle() {
                fileName = fileName,
                fileState = FileItemStatus.NoInfo
            };
            
            if (path != null) {
                result.fileState = EstablishFile(path, fileName);
                result.localPath = path;
                result.fullFilePath = Path.Combine(path, fileName);
                if (result.fileState == FileItemStatus.Created) createdFiles.Add(result);
                if (result.fileState == FileItemStatus.Exists || result.fileState == FileItemStatus.Created) return result;
            } 
            if (!insist) return result;

            while (result.fileState != FileItemStatus.Exists || result.fileState != FileItemStatus.Created) { 
                result.fullFilePath = Path.Combine(result.localPath, fileName);
                result.fileState = EstablishFile(result.localPath, fileName);
                if (result.fileState == FileItemStatus.Created) createdFiles.Add(result);
                if (result.fileState == FileItemStatus.Exists || result.fileState == FileItemStatus.Created) return result;
            }

            return result;
        }
        
        static private bool DeleteHandle(FileHandle target) {
// For now this will not be used, because the file's are being used
// in the moment of the supposed deletion, which leads to exception
            bool isWrapped = string.IsNullOrWhiteSpace(target.wrappingDirectory);

            if (isWrapped) {
                string directoryPath = Path.Combine(target.localPath, target.wrappingDirectory);
                try {
                    if (File.Exists(target.fullFilePath)) {
                        File.Delete(target.fullFilePath);
                        if (Directory.Exists(directoryPath)) {
                            Directory.Delete(directoryPath);
                            return true;
                        }
                    } else {
                        return false;
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    // Console.ReadKey();
                    return false;
                }
            }
            try {
                if (File.Exists(target.fullFilePath)) {
                    File.Delete(target.fullFilePath);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
                // Console.ReadKey();
                return false;
            }
            
            return false;
        }

        static internal void ClearCreatedHandles() {
// Don't use because of concerns related to DeleteHandle(FileHandle)
            for (int i = 0; i < createdFiles.Count; i++) {
                Console.WriteLine($"File: {createdFiles[i].fullFilePath}. Status: {createdFiles[i].fileState}.");
                if (createdFiles[i].fileState == FileItemStatus.Created) {
                    DeleteHandle(createdFiles[i]);
                }
            }
            Console.ReadKey();
        }

        static private (FileItemStatus directoryStatus, FileItemStatus fileStatus) TryCreateNestedFile(string dirPath, string dirName, string fileName) {
            FileItemStatus directoryStatus;
            FileItemStatus fileStatus;

            directoryStatus = EstablishDirectory(dirPath, dirName);
            if (directoryStatus == FileItemStatus.Denied) return (FileItemStatus.Denied, FileItemStatus.Retracted);
                
            fileStatus = EstablishFile(Path.Combine(dirPath, dirName), fileName);
            if (fileStatus == FileItemStatus.Denied) {
                if (directoryStatus == FileItemStatus.Created) {
                    Directory.Delete(Path.Combine(dirPath, dirName));
                    directoryStatus = FileItemStatus.Retracted;
                }
            }
            return (directoryStatus, fileStatus);
        }

        static private FileItemStatus EstablishDirectory(string? basePath, string createDirectory){
            if (CheckDirectoryWritePermission(basePath) != null) {
#pragma warning disable CS8604 // Null reference not possible
                string targetPath = Path.Combine(basePath, createDirectory);
#pragma warning restore CS8604
                if (Directory.Exists(targetPath)) return FileItemStatus.Exists;
                try {
                    Directory.CreateDirectory(targetPath);
                    return Directory.Exists(targetPath) ? FileItemStatus.Created : FileItemStatus.Denied;
                } catch {
                    return FileItemStatus.Denied;
                }
            }
            return FileItemStatus.Denied;
        }

        internal Span<byte> ReadFile(long readFrom, long readTo) {
            long fileLength = new FileInfo(fullFilePath).Length;
            if (readTo > fileLength) readTo = fileLength;
            if (readFrom < 0) readFrom = 0;
            Span<byte> result = new byte[(int)(readTo - readFrom)];
            FileStream fs = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(readFrom, SeekOrigin.Begin);
            fs.Read(result);
            return result;
        }
        
        static private string? CheckDirectoryWritePermission(string? basePath) {
            if (string.IsNullOrWhiteSpace(basePath)) return null;
            if (!Directory.Exists(basePath)) return null;
            string testDir = Path.Combine(basePath, $"WRITE_PROBE_{Guid.NewGuid():N}");
            try {
                Directory.CreateDirectory(testDir);
                if (Directory.Exists(testDir)) {
                    Directory.Delete(testDir);
                    return basePath;
                } else return null;
            } catch {
                return null;
            }
        }

        static private FileItemStatus EstablishFile(string? basePath, string targetFile) {
            if (CheckDirectoryWritePermission(basePath) == null) return FileItemStatus.Denied;
#pragma warning disable CS8604 // Null reference not possible
            string targetPath = Path.Combine(basePath, targetFile);
#pragma warning restore CS8604
            if (File.Exists(targetPath)) return FileItemStatus.Exists;
            try {
                File.Create(targetPath);
                return File.Exists(targetPath) ? FileItemStatus.Created : FileItemStatus.Denied;
            } catch {
                return FileItemStatus.Denied;
            }
        }
    }
}
