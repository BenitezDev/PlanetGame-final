using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  System.IO;
using  System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem 
{
    public static void SaveBandit(Bandit bandit)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        //string path = Application.persistentDataPath + "/bandit.txt";
        string path = Application.streamingAssetsPath + "/bandit.txt";

        FileStream stream = new FileStream(path, FileMode.Create);

        BandidData data = new BandidData(bandit);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static BandidData LoadBandid()
    {
        //string path = Application.persistentDataPath + "/bandit.txt";
        string path = Application.streamingAssetsPath + "/bandit.txt";

        Debug.Log(path);
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            BandidData data = formatter.Deserialize(stream) as BandidData;
            stream.Close();

            return data;
        }
        else
        {
            Debug.Log("No BanditData in " + path);
            return null;
        }
    }
}
