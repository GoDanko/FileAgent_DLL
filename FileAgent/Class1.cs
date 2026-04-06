namespace FileAgent 
{
    public class FSItemHandle
    {
        public string name = "";
        public string localPath = "";
        internal bool isDirectory = false;
        internal DiagnosticState diagnosticData;
        
        [Flags]
        public enum Permissions : byte {
            None = 0,
            Read = 1 << 0,
            Write = 1 << 1,
            Delete = 1 << 2,

            ReadWrite = Read | Write,
            Full = Read | Write | Delete
        }

        internal FSItemHandle() {
            diagnosticData = new DiagnosticState();
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
                target.diagnosticData.PushException(ex);
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

       internal bool HasNestedContent(string targetString) { 
            foreach (char letter in targetString) {
                if (letter == '/' || letter == '\\') return true;
            }
            return false;
        }

    }
    
    public class DirHandle : FSItemHandle {
        
        internal Permissions fileRights;
        internal Permissions dirRights;
        List<FSItemHandle> DirContent = new List<FSItemHandle> ();
 
        static public FSItemHandle EstablishDirAccess(string dirName, string path, bool interruptIfFailure = false) {
            DirHandle result = EstablishDirectory(dirName, path);
            if (!Directory.Exists(Path.Combine(path, dirName))) {
                result.diagnosticData.PushException("HANDLE CREATION error. FIleSystem reports file as missing, handle unreliable");
                if (interruptIfFailure) throw result.diagnosticData.logs[result.diagnosticData.logs.Count - 1];
            }
            result.ProbeAccessRights();
            return result;
        }

        public FSItemHandle? RegisterChildItem(string targetName) {
            if (HasNestedContent(targetName)) {
                diagnosticData.PushException($"ADDING HANDLE error. Adding {targetName} failed, Nested content disallowed.");
                return null;
            }

            if (Directory.Exists(Path.Combine(localPath, name, targetName))) { 
                return DirHandle.EstablishDirAccess(Path.Combine(localPath, name), targetName);
            } else if (File.Exists(Path.Combine(localPath, name, targetName))) { 
                return FileHandle.EstablishFileAccess(Path.Combine(localPath, name), targetName);
            }
            diagnosticData.PushException($"ADDING HANDLE error. {targetName} doesn't exist: create it first");
            return null;
        }

        public List<FSItemHandle>? RegisterAllChilds() {
            IEnumerator<string> containedItems;

            try {
                containedItems = Directory.EnumerateFileSystemEntries(Path.Combine(localPath, name)).GetEnumerator();
            } catch (Exception ex) {
                diagnosticData.PushException(ex);
                return null;
            }
           
            List<FSItemHandle> result = new List<FSItemHandle>();
            while (true) {
                try {
                    if (!containedItems.MoveNext()) {
                        break;
                    } else {
                        FSItemHandle? fsItem = RegisterChildItem(containedItems.Current);
                        if (fsItem != null) result.Add(fsItem);
                    }
                } catch (Exception ex) {
                    diagnosticData.PushException(ex);
                }
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
                    result.diagnosticData.outcome = DiagnosticState.FileItemStatus.Exists;
                    return result;
                } else {
                    try {
                        Directory.CreateDirectory(targetPath);
                        result.diagnosticData.outcome = Directory.Exists(targetPath) ? DiagnosticState.FileItemStatus.Created : DiagnosticState.FileItemStatus.Denied;
                    } catch (Exception ex) {
                        result.diagnosticData.PushException(ex);
                        result.diagnosticData.outcome = DiagnosticState.FileItemStatus.Denied;
                    }
                }
            }
            return result;
        }

        public void ProbeAccessRights() {
            string testDir = Path.Combine(localPath, name, $"FileAgentDLL_TESTDIR{Guid.NewGuid():N}");
            string testFile = Path.Combine(testDir, $"FileAgentDLL_TESTFILE{Guid.NewGuid():N}");

            bool createdDir = false;
            try {
                Directory.CreateDirectory(testDir);
                createdDir = true;
            } catch {}
            if (createdDir) dirRights |= Permissions.Write;
            
            bool createdFile = true;
            bool readFromFile = true;
            try {
                Span<byte> probeTxt = stackalloc byte[] {0x74,0x65,0x73,0x74}; // "test" in UTF8/ASCII
                File.WriteAllBytes(testFile, probeTxt.ToArray());
                Span<byte> readProbe = File.ReadAllBytes(testFile);
                for (byte i = 0; i < probeTxt.Length; i++) {
                    if (probeTxt[i] != readProbe[i]) readFromFile = false;
                } 
            } catch {
                readFromFile = false;
            }
            if (createdFile) {
                fileRights |= Permissions.Write;
                if (readFromFile) {
                    dirRights |= Permissions.Read;
                    fileRights |= Permissions.Read;
                }
            }
            
            bool deletedDir = false;
            bool deletedFile = false;
            try {
                Directory.Delete(testDir);
                deletedDir = true;
            } catch {}
            try {
                File.Delete(testFile);
                deletedFile = true;
            } catch {}
            if (deletedFile) {fileRights |= Permissions.Delete;}
            if (deletedDir) {dirRights |= Permissions.Delete;}
        }
    }

    public class FileHandle : FSItemHandle {
        
        internal Permissions accessRights;

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
            FileHandle result = EstablishFile(fileName, path);
            if (!File.Exists(Path.Combine(path, fileName))) {
                result.diagnosticData.PushException("FIle doesn't exist: creation/retrieval failed");
                if (interruptIfFailure) throw result.diagnosticData.logs[result.diagnosticData.logs.Count - 1];
            }
            result.ProbeAccessRights();
            return result;
        }
 
        static private FileHandle EstablishFile(string fileName, string path){
            FileHandle result = new FileHandle() {
                name = fileName,
                localPath = path,
                isDirectory = false
            };
            if (CheckDirectoryWritePermission(path) != null) {
                string targetPath = Path.Combine(path, fileName);
                if (File.Exists(targetPath)) {
                    result.diagnosticData.outcome = DiagnosticState.FileItemStatus.Exists;
                    return result;
                } else {
                    try {
                        using (File.Create(targetPath)) {};
                        result.diagnosticData.outcome = File.Exists(targetPath) ? DiagnosticState.FileItemStatus.Created : DiagnosticState.FileItemStatus.Denied;
                    } catch (Exception ex) {
                        result.diagnosticData.PushException(ex);
                        result.diagnosticData.outcome = DiagnosticState.FileItemStatus.Denied;
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
