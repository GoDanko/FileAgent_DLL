namespace FileAgent 
{
    public class FSItemHandle
    {
        public string name = "";
        public string localPath = "";
        internal bool isDirectory = false;
        internal DiagnosticState itemDiagnostics;
        internal Permissions accessRights;

        public enum Permissions {
            None,
            Read,
            Write,
            ReadWrite
        }

        internal FSItemHandle() {
            itemDiagnostics = new DiagnosticState();
        }

        static public bool DeleteHandle(FSItemHandle target) {
            string handlePath = Path.Combine(target.localPath, target.name);

            try {
                if (File.Exists(handlePath)) {
                    File.Delete(handlePath);
                }
                if (!File.Exists(handlePath)) {
                    return true;
                }
            } catch (Exception ex) {
                target.itemDiagnostics.PushException(ex);
            }
            
            return false;
        }
      
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
                result.itemDiagnostics.PushException("FIle doesn't exist: creation/retrieval failed");
                if (interruptIfFailure) throw result.itemDiagnostics.logs[result.itemDiagnostics.logs.Count - 1];
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
                    result.itemDiagnostics.outcome = DiagnosticState.FileItemStatus.Exists;
                    return result;
                } else {
                    try {
                        Directory.CreateDirectory(targetPath);
                        result.itemDiagnostics.outcome = Directory.Exists(targetPath) ? DiagnosticState.FileItemStatus.Created : DiagnosticState.FileItemStatus.Denied;
                    } catch (Exception ex) {
                        result.itemDiagnostics.PushException(ex);
                        result.itemDiagnostics.outcome = DiagnosticState.FileItemStatus.Denied;
                    }
                }
            }
            return result;
        }
    }

    class FileHandle : FSItemHandle {

        public void ProbeAccessRights() {
            string targetPath = Path.Combine(localPath, name);
            if (File.Exists(targetPath)) {
                bool foundResult = false;
                if (!foundResult) 
                    try {
                        using FileStream probing = new FileStream(targetPath, FileMode.Open, FileAccess.ReadWrite);
                        accessRights = Permissions.ReadWrite;
                        probing.Dispose();
                        foundResult = true;
                    } catch {}

                if (!foundResult) 
                    try {
                        using FileStream probing = new FileStream(targetPath, FileMode.Open, FileAccess.Read);
                        accessRights = Permissions.Read;
                        probing.Dispose();
                        foundResult = true; 
                    } catch {} 

                if (!foundResult){
                    try {
                        using FileStream probing = new FileStream(targetPath, FileMode.Open, FileAccess.Write);
                        accessRights = Permissions.Write;
                        probing.Dispose();
                        foundResult = true;
                    } catch {}
                }
                accessRights = Permissions.None;
            }
        }

        static public FSItemHandle EstablishFileAccess(string fileName, string path, bool interruptIfFailure = false) {
            DirHandle result = EstablishFile(fileName, path);
            if (!File.Exists(Path.Combine(path, fileName))) {
                result.itemDiagnostics.PushException("FIle doesn't exist: creation/retrieval failed");
                if (interruptIfFailure) throw result.itemDiagnostics.logs[result.itemDiagnostics.logs.Count - 1];
            }
            return result;
        }
 
        static private DirHandle EstablishFile(string fileName, string path){
            DirHandle result = new DirHandle() {
                name = fileName,
                localPath = path,
                isDirectory = false
            };
            if (CheckDirectoryWritePermission(path) != null) {
                string targetPath = Path.Combine(path, fileName);
                if (File.Exists(targetPath)) {
                    result.itemDiagnostics.outcome = DiagnosticState.FileItemStatus.Exists;
                    return result;
                } else {
                    try {
                        File.Create(targetPath).Dispose();
                        result.itemDiagnostics.outcome = File.Exists(targetPath) ? DiagnosticState.FileItemStatus.Created : DiagnosticState.FileItemStatus.Denied;
                    } catch (Exception ex) {
                        result.itemDiagnostics.PushException(ex);
                        result.itemDiagnostics.outcome = DiagnosticState.FileItemStatus.Denied;
                    }
                }
            }
            return result;
        }

        internal Span<byte> ReadFile(long readFrom, long readTo) {
            long fileLength = new FileInfo(Path.Combine(localPath, name)).Length;
            if (readTo > fileLength) readTo = fileLength;
            if (readFrom < 0) readFrom = 0;
            Span<byte> result = new byte[(int)(readTo - readFrom)];
            FileStream fs = new FileStream(Path.Combine(localPath, name), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(readFrom, SeekOrigin.Begin);
            fs.Read(result);
            return result;
        }
   }

    internal class DiagnosticState {
        bool ReadyToWrite = false;
        internal List<Exception> logs = new List<Exception> ();
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
            logs.Add(ex);
        }

        internal void PushException(string exMsg) {
            logs.Add(new Exception(exMsg));
        }
    }
}
