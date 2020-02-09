using UnityEngine;

namespace Voxelmetric
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        //protected static bool s_instanceIsNull = true;

        private static readonly object sLock = new object();

        // Unity objects cannot be compared outside the main thread. Because of that we use a helper
        // variable which tells whether the object instance has already been created. First call has
        // to be made from the main thread, though.
        private static T instance;

        public bool destroyOnLoad = false;

        // Protected constructor, access possible via Instance field only
        protected MonoSingleton()
        {
        }

        public static T Instance
        {
            get
            {
                lock (sLock)
                {
                    // No instance of this class created yet
                    if (instance != null)
                    {
                        return instance;
                    }

                    //if (s_instanceIsNull) {
                    // Only one instance of the object is allowed. Anything else is an error
                    //if (FindObjectsOfType<T>().Length>1)
                    //    throw new Exception("More than one instance of a singleton object exists");

                    // Find an instance of this class in the project
                    // If no other instance is found create a new object
                    instance = FindObjectOfType<T>();
                    if (instance != null)
                    {
                        return instance;
                    }

                    //s_instanceIsNull = false;

                    GameObject go = new GameObject("Singleton " + typeof(T));
                    instance = go.AddComponent<T>();

                    if (!instance.destroyOnLoad)
                    {
                        DontDestroyOnLoad(go);
                    }
                    //else
                    //	s_instanceIsNull = false;

                    return instance;
                }
            }
        }
    }
}
