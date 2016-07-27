using System.Collections.Generic;

namespace Sol2E.Utils
{
    /// <summary>
    /// Multiton to manage identifiers of different clients.
    /// Using the multiton pattern there is one pool per key,
    /// instead of a single id pool for all clients.
    /// Use it like this: 
    /// 
    /// IDPool.GetInstance(key)
    /// </summary>
    public class IDPool : Multiton<IDPool>
    {
        private static List<int> dummyList = new List<int>();

        public const int StartID = 0;
        public const int InvalidID = StartID - 1;

        // provide ids as simple integers, by incrementing this value
        // each time an id is required
        private int _lowestUnassignedID;

        // private constructor, invoked through InstanceCreator
        private IDPool()
        {
            _lowestUnassignedID = StartID;
        }

        /// <summary>
        /// Retrieves the next available id in this pool
        /// </summary>
        /// <returns>an unassigned id for this pool</returns>
        public int GetNextAvailableID()
        {
            return GetNextAvailableID(dummyList);
        }

        /// <summary>
        /// Retrieves the next available id in this pool
        /// </summary>
        /// <param name="existingIds">a list of occupied ids. Will be used if 
        /// lowestUnassignedID reaches int.MaxValue. This should hardly ever happen.</param>
        /// <returns>an unassigned id for this pool</returns>
        public int GetNextAvailableID(ICollection<int> existingIds)
        {
            int result = InvalidID;
            if (_lowestUnassignedID < int.MaxValue)
            {
                // get next available id and increment counter
                result = _lowestUnassignedID;
                _lowestUnassignedID++;
            }
            else
            {
                int i;
                // if more than 2.147.483.647 ids were assigned, we search the list of
                // existing ids, to see if any of those are vacant again
                for (i = 0; i < int.MaxValue; i++)
                {
                    if (!existingIds.Contains(i))
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
