using FileAgent;
using System;
using Xunit;

namespace FileAgent.test;

public class UnitTest1
{

    [Fact]
    public void EstablishFileAccess_FileCreated_Created() {
        (string name, string path) data = ($"FileAgent_TESTFILE1_{Guid.NewGuid():N}", Path.GetTempPath());

	    FileHandle testObject1 = FileHandle.EstablishFileAccess(data.name, data.path, false);
        DiagnosticState.FileItemStatus expectedStatus = DiagnosticState.FileItemStatus.Created;

        Assert.True(File.Exists(Path.Combine(data.path, data.name)));
        Assert.Equal(testObject1.fileState, expectedStatus);
        
        try {
            File.Delete(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 1: Couldn't clean {Path.Combine(data.path, data.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void EstablishFileAccess_FileExists_Exists() {
        (string name, string path) data = ($"FileAgent_TESTFILE2_{Guid.NewGuid():N}", Path.GetTempPath());

        try {
            File.Create(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 2: inconclusive. \n because: {ex}");
        }

        Assert.True(File.Exists(Path.Combine(data.path, data.name)));

        FileHandle testObject2 = FileHandle.EstablishFileAccess(data.name, data.path, false);
        DiagnosticState.FileItemStatus expectedStatus = DiagnosticState.FileItemStatus.Exists;

        Assert.Equal(testObject2.fileState, expectedStatus);

        try {
            File.Delete(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 2: Couldn't clean {Path.Combine(data.path, data.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void EstablishDirAccess_DirCreated_Created() {
        (string name, string path) data = ($"FIleAgent_TESTDIR1_{Guid.NewGuid():N}", Path.GetTempPath());

	    FileHandle testObject3 = FileHandle.EstablishDirAccess(data.name, data.path, false);
        DiagnosticState.FileItemStatus expectedStatus = DiagnosticState.FileItemStatus.Created;
        
        Assert.True(Directory.Exists(Path.Combine(data.path, data.name)));
        Assert.Equal(testObject3.fileState, expectedStatus);

        try {
            Directory.Delete(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 3: Couldn't clean {Path.Combine(data.path, data.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void EstablishDirAccess_DirExists_Exists() {
        (string name, string path) data = ($"FIleAgent_TESTDIR2_{Guid.NewGuid():N}", Path.GetTempPath());
        try {
            Directory.CreateDirectory(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 4: inconclusive. \n because: {ex}");
        }

        Assert.True(Directory.Exists(Path.Combine(data.path, data.name)));

        FileHandle testObject4 = FileHandle.EstablishDirAccess(data.name, data.path, false);
        DiagnosticState.FileItemStatus expectedStatus = DiagnosticState.FileItemStatus.Exists;

        Assert.Equal(testObject4.fileState, expectedStatus);

        try {
            Directory.Delete(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 4: Couldn't clean {Path.Combine(data.path, data.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void DeleteFile_FileMissing() {
        (string name, string path) data = ($"FileAgent_TESTFILE3_{Guid.NewGuid():N}", Path.GetTempPath());
        try {
            File.Create(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 2: inconclusive. \n because: {ex}");
        }

        FileHandle testObject5 = FileHandle.EstablishFileAccess(data.name, data.path, false);
        FileHandle.DeleteHandle(testObject5);

        Assert.False(File.Exists(Path.Combine(data.path, data.name)));
    }

    [Fact]
    public void DeleteDir_DirectoryMissing() {
        (string name, string path) data = ($"FileAgent_TESTDIR3_{Guid.NewGuid():N}", Path.GetTempPath());
        try {
            Directory.CreateDirectory(Path.Combine(data.path, data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 6: inconclusive. \n because: {ex}");
        }

        FileHandle testObject6 = FileHandle.EstablishDirAccess(data.name, data.path, false);
        FileHandle.DeleteHandle(testObject6);
        
        Assert.False(Directory.Exists(Path.Combine(data.path, data.name)));
    }
   }
