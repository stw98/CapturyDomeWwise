using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyExtensions
{
    public class ConverterExtension
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

    public static class TransformExtension
    {
    	public static List<Transform> GetAllChildren(this Transform transform)
    	{
    		List<Transform> children = new();
    		foreach (Transform child in transform) {
    			children.Add(child);
    			children.AddRange(GetAllChildren(child));
    		}
    		return children;
    	}

        public static Dictionary<Transform, string> GetAllChildrenWithName(this Transform transform, Dictionary<Transform, string> children)
        {
            if (children == null)
                children = new Dictionary<Transform, string>();
            foreach (Transform child in transform)
            {
                children.Add(child, child.name);
                child.GetAllChildrenWithName(children);
            }
            return children;
        }
    }

    public class InitializerExtension<T> : MonoBehaviour where T : Component
    {
        public static T InitializeComponent(GameObject gameObject)
        {
            T component;
            if (!gameObject.TryGetComponent<T>(out component))
                component = gameObject.AddComponent<T>();

            return component;
        }
    }
}
