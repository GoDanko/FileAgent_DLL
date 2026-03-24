using FileAgent;
using System;

namespace FileAgent.test;

public class UnitTest1
{
    [Fact]
    public void EstablishNestedFileConnection_FileCreated_Created() {
        string[] file1Data = new string[3];
        file1Data[0] = $"TEST1_{Guid.NewGuid():N}";
        file1Data[1] = $"DIR1_{Guid.NewGuid():N}";
        file1Data[2] = Path.GetTempPath();
        	
	    FileHandle testObject1 = FileHandle.EstablishNestedFileConnection(file1Data[0], file1Data[1], file1Data[2], false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Created;
        
        Assert.True(Directory.Exists(Path.Combine(file1Data[2], file1Data[1])));
        Assert.True(File.Exists(Path.Combine(file1Data[2], file1Data[1], file1Data[0])));
        Assert.Equal(testObject1.fileState, expectedStatus);

        File.Delete(Path.Combine(file1Data[2], file1Data[1], file1Data[0]));
        Directory.Delete(Path.Combine(file1Data[2], file1Data[1]));
    }

    [Fact]
    public void EstablishNestedFileConnection_FileExists_Exists() {
        string[] file2Data = new string[3];
        file2Data[0] = $"TEST2_{Guid.NewGuid():N}";
        file2Data[1] = $"DIR2_{Guid.NewGuid():N}";
        file2Data[2] = Path.GetTempPath();
        try {
            Directory.CreateDirectory(Path.Combine(file2Data[2], file2Data[1]));
            File.Create(Path.Combine(file2Data[2], file2Data[1], file2Data[0]));
        } catch (Exception ex) {
            throw new Exception("Test inconclusive because: ", ex);
        }

        FileHandle testObject2 = FileHandle.EstablishNestedFileConnection(file2Data[0], file2Data[1], file2Data[2], false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Exists;

        Assert.Equal(testObject2.fileState, expectedStatus);

        File.Delete(Path.Combine(file2Data[2], file2Data[1], file2Data[0]));
        Directory.Delete(Path.Combine(file2Data[2], file2Data[1]));
    }

    [Fact]
    public void EstablishFileConnection_FileCreated_Created() {
        string[] file3Data = new string[2];
        file3Data[0] = $"TEST3_{Guid.NewGuid():N}";
        file3Data[1] = Path.GetTempPath();
        	
	    FileHandle testObject3 = FileHandle.EstablishFileConnection(file3Data[0], file3Data[1], false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Created;
        
        Assert.True(File.Exists(Path.Combine(file3Data[1], file3Data[0])));
        Assert.Equal(testObject3.fileState, expectedStatus);

        File.Delete(Path.Combine(file3Data[1], file3Data[0]));
    }

    [Fact]
    public void EstablishFileConnection_FileExists_Exists() {
        string[] file4Data = new string[2];
        file4Data[0] = $"TEST4_{Guid.NewGuid():N}";
        file4Data[1] = Path.GetTempPath();
        try {
            File.Create(Path.Combine(file4Data[1], file4Data[0]));
        } catch (Exception ex) {
            throw new Exception("Test inconclusive because: ", ex);
        }

        FileHandle testObject4 = FileHandle.EstablishFileConnection(file4Data[0], file4Data[1], false);
        FileHandle.FileItemStatus expectedStatus = FileHandle.FileItemStatus.Exists;

        Assert.Equal(testObject4.fileState, expectedStatus);

        File.Delete(Path.Combine(file4Data[1], file4Data[0]));
    }


}
