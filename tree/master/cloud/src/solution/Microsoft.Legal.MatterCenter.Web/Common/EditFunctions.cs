﻿using Microsoft.Extensions.Options;
using Microsoft.Legal.MatterCenter.Models;
using Microsoft.Legal.MatterCenter.Repository;
using Microsoft.Legal.MatterCenter.Utility;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Legal.MatterCenter.Web.Common
{
    public class EditFunctions : IEditFunctions
    {
        private MatterSettings matterSettings;
        private ErrorSettings errorSettings;
        private ListNames listNames;
        private CamlQueries camlQueries;
        private ISPList spList;
        private IMatterRepository matterRespository;
        public EditFunctions(ISPList spList, IOptionsMonitor<MatterSettings> matterSettings, 
            IOptionsMonitor<ErrorSettings> errorSettings, IMatterRepository matterRespository,
            IOptionsMonitor<ListNames> listNames, IOptionsMonitor<CamlQueries> camlQueries)
        {
            this.matterSettings = matterSettings.CurrentValue;
            this.spList = spList;
            this.errorSettings = errorSettings.CurrentValue;
            this.matterRespository = matterRespository;
            this.listNames = listNames.CurrentValue;
            this.camlQueries = camlQueries.CurrentValue;
        }

        public GenericResponseVM CheckSecurityGroupInTeamMembers(Client client, Matter matter, IList<string> userId)
        {
            try
            {
                GenericResponseVM genericResponse = null;
                int securityGroupRowNumber = -1; // Blocked user field has security group
                List<Tuple<int, Principal>> teamMemberPrincipalCollection = new List<Tuple<int, Principal>>();
                if (null != matter && null != matter.AssignUserNames && null != matter.BlockUserNames)
                {                   
                    teamMemberPrincipalCollection = matterRespository.CheckUserSecurity(client, matter, userId);
                    foreach (Tuple<int, Principal> teamMemberPrincipal in teamMemberPrincipalCollection)
                    {
                        Principal currentTeamMemberPrincipal = teamMemberPrincipal.Item2;
                        if (currentTeamMemberPrincipal.PrincipalType == PrincipalType.SecurityGroup)
                        {
                            securityGroupRowNumber = teamMemberPrincipal.Item1;
                            return ServiceUtility.GenericResponse(errorSettings.ErrorCodeSecurityGroupExists,
                                errorSettings.ErrorMessageSecurityGroupExists + ServiceConstants.DOLLAR + userId[securityGroupRowNumber]);
                        }
                    }  
                }
                else
                {
                    return ServiceUtility.GenericResponse(errorSettings.IncorrectTeamMembersCode,
                                    errorSettings.IncorrectTeamMembersMessage);
                }
                return genericResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Validates if there is at-least one user with full control in assign list.
        /// </summary>
        /// <param name="matter">Matter object</param>
        /// <returns>Status of Full Control permission</returns>
        public bool ValidateFullControlPermission(Matter matter)
        {

            bool hasFullConrol = false;
            if (null != matter && null != matter.Permissions && 0 != matter.Permissions.Count)
            {
                hasFullConrol = matter.Permissions.Contains(matterSettings.EditMatterAllowedPermissionLevel);
            }
            return hasFullConrol;
        }

        
    }
}