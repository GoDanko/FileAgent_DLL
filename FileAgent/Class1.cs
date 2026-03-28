namespace FileAgent 
{
    public class FSItemHandle
    {
        public string name = "";
        public string localPath = "";
        internal FileState fileDiagnostics;


        internal FSItemHandle() {
            fileDiagnostics = new FileState();
        }

        static public FSItemHandle EstablishNestedFileConnection(string name, string wrapinDirectory, string? path = null, bool insist = true) {
            FSItemHandle result = new FSItemHandle() {
                name = name,
                wrappingDirectory = wrapinDirectory,
                fileDiagnostics = FileItemStatus.NoInfo
            };

            if (path != null) {
                (FileItemStatus directoryStatus, FileItemStatus fileStatus) status = TryCreateNestedFile(path, wrapinDirectory, name);
                result.fileDiagnostics = status.fileStatus;
                result.localPath = path;
                result.fullFilePath = Path.Combine(path, wrapinDirectory, name);
                if (result.fileDiagnostics == FileItemStatus.Exists || result.fileDiagnostics == FileItemStatus.Created) return result;
            }
            if (!insist) return result;

            while (result.fileDiagnostics != FileItemStatus.Exists || result.fileDiagnostics != FileItemStatus.Created) { 
                (FileItemStatus directoryStatus, FileItemStatus fileStatus) status = TryCreateNestedFile(result.localPath, wrapinDirectory, name);
                result.fullFilePath = Path.Combine(result.localPath, wrapinDirectory, name);
                result.fileDiagnostics = status.fileStatus;
                if (result.fileDiagnostics == FileItemStatus.Exists || result.fileDiagnostics == FileItemStatus.Created) return result;
            }

            return result;
        }
        
        static public FSItemHandle EstablishFileConnection(string name, string? path = null, bool insist = false) {
            FSItemHandle result = new FSItemHandle() {
                name = name,
                fileDiagnostics = FileItemStatus.NoInfo
            };
            
            if (path != null) {
                result.fileDiagnostics = EstablishFile(path, name);
                result.localPath = path;
                result.fullFilePath = Path.Combine(path, name);
                if (result.fileDiagnostics == FileItemStatus.Created) createdFiles.Add(result);
                if (result.fileDiagnostics == FileItemStatus.Exists || result.fileDiagnostics == FileItemStatus.Created) return result;
            } 
            if (!insist) return result;

            while (result.fileDiagnostics != FileItemStatus.Exists || result.fileDiagnostics != FileItemStatus.Created) { 
                result.fullFilePath = Path.Combine(result.localPath, name);
                result.fileDiagnostics = EstablishFile(result.localPath, name);
                if (result.fileDiagnostics == FileItemStatus.Created) createdFiles.Add(result);
                if (result.fileDiagnostics == FileItemStatus.Exists || result.fileDiagnostics == FileItemStatus.Created) return result;
            }

            return result;
        }

        
        
        static public bool DeleteHandle(FSItemHandle target) {
// For now this will not be used, because the file's are being used
// in the moment of the supposed deletion, which leads to exception
            bool isWrapped = !string.IsNullOrWhiteSpace(target.wrappingDirectory);

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


/*        static private (FileItemStatus directoryStatus, FileItemStatus fileStatus) TryCreateNestedFile(string dirPath, string dirName, string name) {
            FileItemStatus directoryStatus;
            FileItemStatus fileStatus;

            directoryStatus = EstablishDirectory(dirPath, dirName);
            if (directoryStatus == FileItemStatus.Denied) return (FileItemStatus.Denied, FileItemStatus.Retracted);
                
            fileStatus = EstablishFile(Path.Combine(dirPath, dirName), name);
            if (fileStatus == FileItemStatus.Denied) {
                if (directoryStatus == FileItemStatus.Created) {
                    Directory.Delete(Path.Combine(dirPath, dirName));
                    directoryStatus = FileItemStatus.Retracted;
                }
            }
            return (directoryStatus, fileStatus);
        } */
       
        static internal string? CheckDirectoryWritePermission(string? basePath) {
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

    }
    
    class DirHandle : FSItemHandle {
 
        static public FSItemHandle EstablishDirAccess(string dirName, string path, bool interruptIfFailure = false) {
            DirHandle result = EstablishDirectory(dirName, path);
            if (!Directory.Exists(Path.Combine(path, dirName))) {
                result.fileDiagnostics.PushException("FIle doesn't exist: creation/retrieval failed");
                if (interruptIfFailure) throw result.fileDiagnostics.collectedThrows[result.fileDiagnostics.collectedThrows.Count - 1];
            }
            return result;
        }


        static private DirHandle EstablishDirectory(string dirName, string path){
            DirHandle result = new DirHandle() {
                name = dirName,
                localPath = path
            };

            if (CheckDirectoryWritePermission(path) != null) {
                string targetPath = Path.Combine(path, dirName);
                if (Directory.Exists(targetPath)) {
                    result.fileDiagnostics.outcome = result.fileDiagnostics.FileItemStatus.Exists;
                    return result;
                } else {
                    try {
                        Directory.CreateDirectory(targetPath);
                        result.fileDiagnostics.outcome = Directory.Exists(targetPath) ? result.fileDiagnostics.FileItemStatus.Created : result.fileDiagnostics.FileItemStatus.Denied;
                    } catch (Exception ex) {
                        result.fileDiagnostics.PushException(ex);
                        result.fileDiagnostics.outcome = result.fileDiagnostics;
                    }
                }
            }
            return result;
        }

        
        
    }

    class FileHandle : FSItemHandle {

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

        static private FileItemStatus EstablishFile(string? basePath, string targetFile) {
            if (CheckDirectoryWritePermission(basePath) == null) return FileItemStatus.Denied;
#pragma warning disable CS8604 // Null reference not possible
            string targetPath = Path.Combine(basePath, targetFile);
#pragma warning restore CS8604
            if (File.Exists(targetPath)) return FileItemStatus.Exists;
            try {
                File.Create(targetPath).Dispose();
                return File.Exists(targetPath) ? FileItemStatus.Created : FileItemStatus.Denied;
            } catch {
                return FileItemStatus.Denied;
            }
        }
    }

    internal class FileState {
        bool ReadyToWrite = false;
        internal List<Exception> collectedThrows = new List<Exception> ();
        internal FileItemStatus outcome = FileItemStatus.NoInfo;

        public enum FileItemStatus {
            NoInfo, // Info has yet to be gathered
            Exists, // A File with the same name existed
            Denied, // Failed to create the file
            Missing, // File isn't present during check
            Created, // successfully created the file
            Retracted // A file was to be created, but prevented due to unmet requirements 
        }

        internal void PushException(Exception ex) {
            collectedThrows.Add(ex);
        }

        internal void PushException(string exMsg) {
            collectedThrows.Add(new Exception(exMsg));
        }
    }
}
