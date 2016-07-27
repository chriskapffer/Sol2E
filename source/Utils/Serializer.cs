using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sol2E.Utils
{
    /// <summary>
    /// Generic serialization class, which can serialize every object, which either implements the
    /// ISerializable interface, or is marked with the [Serializable] attribute. 
    /// 
    /// Binary serialization only! Won't work on XBOX!
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Binary serializes a generic object
        /// </summary>
        /// <typeparam name="T">generic type parameter (has to be a reference type)</typeparam>
        /// <param name="objectToSerialize">object to serialize</param>
        /// <returns>serialized data as byte array</returns>
        public static byte[] Serialize<T>(T objectToSerialize) where T : class
        {
            byte[] serializedData;
            using (var memoryStream = new MemoryStream())
            {
                // use DeflateStream to compress the data even further
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
                {
                    var bFormatter = new BinaryFormatter();
                    bFormatter.Serialize(deflateStream, objectToSerialize);
                }
                    serializedData = memoryStream.ToArray();
            }
            return serializedData;
        }

        /// <summary>
        /// Deserializes a byte array as T
        /// </summary>
        /// <typeparam name="T">return type (has to be a reference type)</typeparam>
        /// <param name="dataToDeserialize">serialized byte array</param>
        /// <returns>deserialized data as T</returns>
        public static T Deserialize<T>(byte[] dataToDeserialize) where T : class
        {
            T deserializedObject;
            using (var memoryStream = new MemoryStream(dataToDeserialize))
            {
                // uncompress MemoryStream with DeflateStream
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress, true))
                {
                    var bFormatter = new BinaryFormatter();
                    deserializedObject = (T)bFormatter.Deserialize(deflateStream);
                }
            }
            return deserializedObject;
        }

        /// <summary>
        /// Stores byte array in file
        /// </summary>
        /// <param name="path">path to file. File and directories will be created if necessary</param>
        /// <param name="byteArray">byte array to store</param>
        /// <returns>false if operation fails</returns>
        public static bool ByteArrayToFile(string path, byte[] byteArray)
        {
            var success = true;

            FileStream fileStream = null;
            try
            {
                string directoryName = Path.GetDirectoryName(path);
                if (directoryName != null && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                fileStream = File.Open(path, FileMode.Create, FileAccess.Write);
                fileStream.Write(byteArray, 0, byteArray.Length);
            }
            catch (Exception ex)
            {
                // don't crash, just write to console
                Console.WriteLine(ex.Message);
                success = false;
            }
            finally
            {
                // clean up
                if (fileStream != null)
                    fileStream.Close();
            }
            return success;
        }

        /// <summary>
        /// Restores byte array from file
        /// </summary>
        /// <param name="path">path to file</param>
        /// <param name="byteArray">file content as byte array (out param)</param>
        /// <returns>true if operation succeeds</returns>
        public static bool ByteArrayFromFile(string path, out byte[] byteArray)
        {
            bool success = true;
            byteArray = null;
            FileStream fileStream = null;
            try
            {
                fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
                int streamLength = Convert.ToInt32(fileStream.Length);

                byteArray = new byte[streamLength];
                fileStream.Read(byteArray, 0, streamLength);
            }
            catch (Exception ex)
            {
                // don't crash, just write to console
                Console.WriteLine(ex.Message);
                success = false;
            }
            finally
            {
                // clean up
                if (fileStream != null)
                    fileStream.Close();
            }
            return success;
        }
    }
}
