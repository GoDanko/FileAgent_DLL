using FileAgent;
using System;
using Xunit;

namespace FileAgent.test;

public class UnitTest1
{

    [Fact]
    public void EstablishNestedFileConnection_FileCreated_Created() {
        (string name, string dir, string path) fileData = ($"FileAgent_TESTFILE1_{Guid.NewGuid():N}", $"FIleAgent_TESTDIR1_{Guid.NewGuid():N}", Path.GetTempPath());

	    FileHandle testObject1 = FileHandle.EstablishNestedFileConnection(fileData.name, fileData.dir, fileData.path, false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Created;

        Assert.True(Directory.Exists(Path.Combine(fileData.path, fileData.dir)));
        Assert.True(File.Exists(Path.Combine(fileData.path, fileData.dir, fileData.name)));
        Assert.Equal(testObject1.fileState, expectedStatus);
        
        try {
            File.Delete(Path.Combine(fileData.path, fileData.dir, fileData.name));
            Directory.Delete(Path.Combine(fileData.path, fileData.dir));
        } catch (Exception ex) {
            throw new Exception($"Test 1: Couldn't clean {Path.Combine(fileData.path, fileData.dir, fileData.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void EstablishNestedFileConnection_FileExists_Exists() {
        (string name, string dir, string path) fileData = ($"FileAgent_TESTFILE2_{Guid.NewGuid():N}", $"FIleAgent_TESTDIR2_{Guid.NewGuid():N}", Path.GetTempPath());

        try {
            Directory.CreateDirectory(Path.Combine(fileData.path, fileData.dir));
            File.Create(Path.Combine(fileData.path, fileData.dir, fileData.name));
        } catch (Exception ex) {
            throw new Exception($"Test 2: inconclusive. \n because: {ex}");
        }

        FileHandle testObject2 = FileHandle.EstablishNestedFileConnection(fileData.name, fileData.dir, fileData.path, false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Exists;

        Assert.Equal(testObject2.fileState, expectedStatus);

        try {
            File.Delete(Path.Combine(fileData.path, fileData.dir, fileData.name));
            Directory.Delete(Path.Combine(fileData.path, fileData.dir));
        } catch (Exception ex) {
            throw new Exception($"Test 2: Couldn't clean {Path.Combine(fileData.path, fileData.dir, fileData.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void EstablishFileConnection_FileCreated_Created() {
        (string name, string path) fileData = ($"FileAgent_TESTFILE3_{Guid.NewGuid():N}", Path.GetTempPath());

	    FileHandle testObject3 = FileHandle.EstablishFileConnection(fileData.name, fileData.path, false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Created;
        
        Assert.True(File.Exists(Path.Combine(fileData.path, fileData.name)));
        Assert.Equal(testObject3.fileState, expectedStatus);

        try {
            File.Delete(Path.Combine(fileData.path, fileData.name));
        } catch (Exception ex) {
            throw new Exception($"Test 3: Couldn't clean {Path.Combine(fileData.path, fileData.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void EstablishFileConnection_FileExists_Exists() {
        (string name, string path) fileData = ($"FileAgent_TESTFILE4_{Guid.NewGuid():N}", Path.GetTempPath());
        try {
            File.Create(Path.Combine(fileData.path, fileData.name));
        } catch (Exception ex) {
            throw new Exception($"Test 4: inconclusive. \n because: {ex}");
        }

        FileHandle testObject4 = FileHandle.EstablishFileConnection(fileData.name, fileData.path, false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Exists;

        Assert.Equal(testObject4.fileState, expectedStatus);

        try {
            File.Delete(Path.Combine(fileData.path, fileData.name));
        } catch (Exception ex) {
            throw new Exception($"Test 4: Couldn't clean {Path.Combine(fileData.path, fileData.name)}. \n because: {ex}");
        }
    }

    [Fact]
    public void DeleteHandle_NestedFileDeleted() {
        (string name, string dir, string path) fileData = ($"FileAgent_TESTFILE5_{Guid.NewGuid():N}", $"FIleAgent_TESTDIR5_{Guid.NewGuid():N}", Path.GetTempPath());
        try {
            Directory.CreateDirectory(Path.Combine(fileData.path, fileData.dir));
            File.Create(Path.Combine(fileData.path, fileData.dir, fileData.name));
        } catch (Exception ex) {
            throw new Exception($"Test 2: inconclusive. \n because: {ex}");
        }

        FileHandle testObject5 = FileHandle.EstablishNestedFileConnection(fileData.name, fileData.dir, fileData.path, false);
        FileHandle.DeleteHandle(testObject5);

        Assert.False(File.Exists(Path.Combine(fileData.path, fileData.dir, fileData.name)));
    }

    [Fact]
    public void DeleteHandle_FileDeted() {
        (string name, string path) fileData = ($"FileAgent_TESTFILE6_{Guid.NewGuid():N}", Path.GetTempPath());
        try {
            File.Create(Path.Combine(fileData.path, fileData.name));
        } catch (Exception ex) {
            throw new Exception($"Test 6: inconclusive. \n because: {ex}");
        }

        FileHandle testObject6 = FileHandle.EstablishFileConnection(fileData.name, fileData.path, false);
        FileHandle.DeleteHandle(testObject6);
        
        Assert.False(File.Exists(Path.Combine(fileData.path, fileData.name)));
    }
    
    [Fact]
    public void ClearCreatedHandles_emptyCreatedFilesArray_AllCreated() {
        (string name, string path) file1Data = ($"FileAgent_TESTFILE7_{Guid.NewGuid():N}", Path.GetTempPath());
        (string name, string path) file2Data = ($"FileAgent_TESTFILE8_{Guid.NewGuid():N}", Path.GetTempPath());
        FileHandle testObject7 = FileHandle.EstablishFileConnection(file1Data.name, file1Data.path, false);
        FileHandle testObject8 = FileHandle.EstablishFileConnection(file2Data.name, file2Data.path, false);
        
        if (File.Exists(Path.Combine(file1Data.path, file1Data.name)) && File.Exists(Path.Combine(file2Data.path, file2Data.name))) {
            Assert.Equal(FileHandle.createdFiles.Count, 2);
            FileHandle.ClearCreatedHandles();
            Assert.Equal(FileHandle.createdFiles.Count, 0);
            Assert.False(File.Exists(Path.Combine(file1Data.path, file1Data.name)));
            Assert.False(File.Exists(Path.Combine(file2Data.path, file2Data.name)));
        } else {
            throw new Exception($"Test 7: inconclusive. \n because FileHandle.EstablishFileConnection() failed to establish a file within the host file system.");
        }
    }

    [Fact]
    public void ClearCreatedHandles_emptyCreatedFilesArray_CreatedAndExists() {
        (string name, string path) file1Data = ($"FileAgent_TESTFILE7_{Guid.NewGuid():N}", Path.GetTempPath());
        (string name, string path) file2Data = ($"FileAgent_TESTFILE8_{Guid.NewGuid():N}", Path.GetTempPath());
        try {
            File.Create(Path.Combine(file1Data.path, file1Data.name));
        } catch (Exception ex) {
            throw new Exception($"Test 6: inconclusive. \n because: {ex}");
        }
        FileHandle testObject9 = FileHandle.EstablishFileConnection(file1Data.name, file1Data.path, false);
        FileHandle testObject10 = FileHandle.EstablishFileConnection(file2Data.name, file2Data.path, false);
        
        if (File.Exists(Path.Combine(file1Data.path, file1Data.name)) && File.Exists(Path.Combine(file2Data.path, file2Data.name))) {
            Assert.Equal(testObject9.fileState, FileHandle.FileItemStatus.Existed);
            FileHandle.ClearCreatedHandles();
            Assert.Equal(FileHandle.createdFiles.Count, 0);
            Assert.True(File.Exists(Path.Combine(file1Data.path, file1Data.name)));
            Assert.False(File.Exists(Path.Combine(file2Data.path, file2Data.name)));
        } else {
            throw new Exception($"Test 8: inconclusive. \n because FileHandle.EstablishFileConnection() failed to establish a file within the host file system.");
        }
    } 
}
