using System;
using System.Collections.Generic;
using UnityEngine;

public class MyExtension
{
    /// <summary>
    /// Extension
    /// </summary>
    /// <param name="byteBuffer"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>

    public static float[] BytesToFloats(byte[] byteBuffer)
    {
        // Ensure the byte buffer is a multiple of 4 (since a float is 4 bytes)
        if (byteBuffer.Length % 4 != 0)
        {
            throw new ArgumentException("Byte array length must be a multiple of 4.");
        }

        // The number of floats in the byte buffer
        int floatCount = byteBuffer.Length / 4;

        // Create a float array to hold the results
        float[] floatArray = new float[floatCount];

        // Copy bytes into floats
        for (int i = 0; i < floatCount; i++)
        {
            // Convert 4 bytes into a single float
            floatArray[i] = BitConverter.ToSingle(byteBuffer, i * 4);
        }

        return floatArray;
    }

    public static byte[] FloatsToBytes(float[] floatArray)
    {
        byte[] bytes = new byte[floatArray.Length * 4];

        for (int i = 0; i < floatArray.Length; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(floatArray[i]), 0, bytes, i * 4, 4);
        }

        return bytes;
    }

    public static List<Transform> GetAllChildren(Transform parent, List<Transform> transformList = null)
    {
        if (transformList == null) transformList = new List<Transform>();
        
        foreach (Transform child in parent) {
            transformList.Add(child);
            GetAllChildren(child, transformList);
        }
        return transformList;
    }
}
