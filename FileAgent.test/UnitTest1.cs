using FileAgent;
using System;
using Xunit;

namespace FileAgent.test;

public class UnitTest1
{

/*
        public enum FileItemStatus {
            NoInfo, // Info has yet to be gathered
            Exists, // A File with the same name existed
            Denied, // Failed to create the file
            Missing, // File isn't present during check
            Created, // successfully created the file
            Retracted // A file was to be created, but prevented due to unmet requirements 
        }
*/

    [Fact]
    public void EstablishDirAccess_CreateDir_StateCreated() {

        (string path, string name) data = ($"{Path.GetTempPath()}", $"FileAgent.Testing_Dir0_{Guid.NewGuid():N}");
        DiagnosticState.FileItemStatus expectedOutcome = DiagnosticState.FileItemStatus.Created;

        DirHandle testDir = (DirHandle)DirHandle.EstablishDirAccess(data.name, data.path);
        Assert.True(Directory.Exists(Path.Combine(data.path, data.name)));
        Assert.Equal(expectedOutcome, testDir.diagnosticData.outcome);

        try {
            Directory.Delete(Path.Combine(data.path, data.name));
        } catch (Exception) {
            throw;
        }
    }

    [Fact]
    public void EstablishFileAccess_CreateFile_StateCreated() {

        (string path, string name) data = ($"{Path.GetTempPath()}", $"FileAgent.Testing_File0_{Guid.NewGuid():N}");
        DiagnosticState.FileItemStatus expectedOutcome = DiagnosticState.FileItemStatus.Created;
        
        FileHandle testFile = (FileHandle)FileHandle.EstablishFileAccess(data.name, data.path);
        Assert.True(File.Exists(Path.Combine(data.path, data.name)));
        Assert.Equal(expectedOutcome, testFile.diagnosticData.outcome);

        try {
            File.Delete(Path.Combine(data.path, data.name));
        } catch (Exception) {
            throw;
        }
    }

    [Fact]
    public void EstablishFileAccess_RegisterDir_StateExisted() {

        (string path, string name) data = ($"{Path.GetTempPath()}", $"FileAgent.Testing_Dir1_{Guid.NewGuid():N}");
        try {
            Directory.CreateDirectory(Path.Combine(data.path, data.name));
        } catch (Exception) {
            throw;
        }
        DiagnosticState.FileItemStatus expectedOutcome = DiagnosticState.FileItemStatus.Exists;


        DirHandle testDir = (DirHandle)DirHandle.EstablishDirAccess(data.name, data.path);
        Assert.True(Directory.Exists(Path.Combine(data.path, data.name)));
        Assert.Equal(expectedOutcome, testDir.diagnosticData.outcome);

        try {
            Directory.Delete(Path.Combine(data.path, data.name));
        } catch (Exception) {
            throw;
        }
    }

    [Fact]
    public void EstablishFileAccess_RegisterFile_StateExisted() {

        (string path, string name) data = ($"{Path.GetTempPath()}", $"FileAgent.Testing_File1_{Guid.NewGuid():N}");
        try {
            File.Create(Path.Combine(data.path, data.name));
        } catch (Exception) {
            throw;
        }
        DiagnosticState.FileItemStatus expectedOutcome = DiagnosticState.FileItemStatus.Exists;


        FileHandle testFile = (FileHandle)FileHandle.EstablishFileAccess(data.name, data.path);
        Assert.True(File.Exists(Path.Combine(data.path, data.name)));
        Assert.Equal(expectedOutcome, testFile.diagnosticData.outcome);

        try {
            File.Delete(Path.Combine(data.path, data.name));
        } catch (Exception) {
            throw;
        }
    }

    [Fact]
    public void RegisterAllChilds_PopulateListWithFSItems() {

        (string path, string name) data = ($"{Path.GetTempPath()}", $"FileAgent.Testing_Dir2_{Guid.NewGuid():N}");
        DirHandle testDir = (DirHandle)DirHandle.EstablishDirAccess(data.name, data.path);
        string testPath = Path.Combine(data.path, data.name);
        string[] fileSystemItemPaths = new string[] {
            Path.Combine(testPath, $"FileAgent.TestChildDetection1{Guid.NewGuid():N}"),
            Path.Combine(testPath, $"FileAgent.TestChildDetection2{Guid.NewGuid():N}"),
            Path.Combine(testPath, $"FileAgent.TestChildDetection3{Guid.NewGuid():N}"),
            Path.Combine(testPath, $"FileAgent.TestChildDetection4{Guid.NewGuid():N}")
        };
        try {
            Directory.CreateDirectory(fileSystemItemPaths[0]);
            Directory.CreateDirectory(fileSystemItemPaths[1]);
            File.Create(fileSystemItemPaths[2]);
            File.Create(fileSystemItemPaths[3]);
        } catch {}

        List<FSItemHandle>? result = testDir.RegisterAllChilds();

        if (result != null) {
        Assert.Equal(fileSystemItemPaths.Length, testDir.filesskimmedover);
        Assert.Equal(fileSystemItemPaths.Length, result.Count);
        for (int i = 0; i < result.Count; i++) {
            Assert.Equal(Path.Combine(result[i].localPath, result[i].name), fileSystemItemPaths[i]);
        }
        }

        try {
            Directory.Delete(fileSystemItemPaths[0]);
            Directory.Delete(fileSystemItemPaths[1]);
            File.Delete(fileSystemItemPaths[2]);
            File.Delete(fileSystemItemPaths[3]);
            Directory.Delete(Path.Combine(data.path, data.name));
        } catch {}
    }
}
