/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Reflection;
using System.Text;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;

namespace OpenSim.Framework.Communications
{    
    /// <summary>
    /// Resolves OpenSim Profile Anchors (OSPA).  An OSPA is a string used to provide information for 
    /// identifying user profiles or supplying a simple name if no profile is available.
    /// </summary>
    public class OspResolver
    {   
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public const string OSPA_PREFIX = "ospi:";
        public const string OSPA_NAME_KEY = "n";
        public const string OSPA_NAME_VALUE_SEPARATOR = " ";
        public const string OSPA_TUPLE_SEPARATOR = "|";
        public static readonly char[] OSPA_TUPLE_SEPARATOR_ARRAY = OSPA_TUPLE_SEPARATOR.ToCharArray();
        public const string OSPA_KEY_VALUE_PAIR_SEPARATOR = "=";
        
        /// <summary>
        /// Resolve an osp string into the most suitable internal OpenSim identifier.
        /// </summary>
        /// 
        /// In some cases this will be a UUID if a suitable profile exists on the system.  In other cases, this may
        /// just return the same identifier after creating a temporary profile.
        /// 
        /// <param name="ospa"></param>
        /// <param name="commsManager"></param>
        /// <returns>
        /// A suitable internal OpenSim identifier.  If the input string wasn't ospi data, then we simply
        /// return that same string.  If the input string was ospi data but no valid profile information has been found,
        /// then returns null.
        /// </returns>
        public static string Resolve(string ospa, CommunicationsManager commsManager)
        {
            if (!ospa.StartsWith(OSPA_PREFIX))
                return ospa;
            
            string ospaMeat = ospa.Substring(OSPA_PREFIX.Length);            
            string[] ospaTuples = ospaMeat.Split(OSPA_TUPLE_SEPARATOR_ARRAY);
            
            foreach (string tuple in ospaTuples)
            {
                int tupleSeparatorIndex = tuple.IndexOf(OSPA_TUPLE_SEPARATOR);

                if (tupleSeparatorIndex < 0)
                {
                    m_log.WarnFormat("[OSPA RESOLVER]: Ignoring non-tuple component {0} in OSPA {1}", tuple, ospa);
                    continue;
                }
                
                string key = tuple.Remove(tupleSeparatorIndex).Trim();
                string value = tuple.Substring(tupleSeparatorIndex + 1).Trim();
                
                if (OSPA_NAME_KEY == key)
                    return ResolveOspaName(value, commsManager);
            }
            
            return null;
        }
        
        /// <summary>
        /// Resolve an OSPI name by querying existing persistent user profiles.  If there is no persistent user profile
        /// then a temporary user profile is inserted in the cache.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="commsManager"></param>
        /// <returns>
        /// An OpenSim internal identifier for the name given.  Returns null if the name was not valid
        /// </returns>
        protected static string ResolveOspaName(string name, CommunicationsManager commsManager)
        {
            int nameSeparatorIndex = name.IndexOf(OSPA_NAME_VALUE_SEPARATOR);
            
            if (nameSeparatorIndex < 0)
            {
                m_log.WarnFormat("[OSPA RESOLVER]: Ignoring unseparated name {0}", name);
                return null;
            }
            
            string firstName = name.Remove(nameSeparatorIndex).TrimEnd();
            string lastName = name.Substring(nameSeparatorIndex + 1).TrimStart();
            
            CachedUserInfo userInfo = commsManager.UserProfileCacheService.GetUserDetails(firstName, lastName);
            if (userInfo != null)
                return userInfo.UserProfile.ID.ToString();
            
            UserProfileData tempUserProfile = new UserProfileData();
            tempUserProfile.FirstName = firstName;
            tempUserProfile.SurName = lastName;
            tempUserProfile.ID = new UUID(Utils.MD5(Encoding.Unicode.GetBytes(tempUserProfile.Name)), 0);
            
            commsManager.UserService.AddTemporaryUserProfile(tempUserProfile);
            
            return tempUserProfile.ID.ToString();
        }
    }
}
