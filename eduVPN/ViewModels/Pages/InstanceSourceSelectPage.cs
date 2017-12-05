﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Instance source selection wizard page
    /// </summary>
    public class InstanceSourceSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Select instance source command
        /// </summary>
        public DelegateCommand<InstanceSourceType?> SelectInstanceSource
        {
            get
            {
                if (_select_instance_source == null)
                {
                    _select_instance_source = new DelegateCommand<InstanceSourceType?>(
                        // execute
                        async instance_source_type =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceSourceType = instance_source_type.Value;

                                if (Parent.InstanceSource is LocalInstanceSource)
                                {
                                    // With local authentication, the authenticating instance is the connecting instance.
                                    // Therefore, select "authenticating" instance.
                                    Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                }
                                else if (Parent.InstanceSource is DistributedInstanceSource instance_source_distributed)
                                {
                                    // Check if we have saved access token for any of the instances.
                                    object authenticating_instance_lock = new object();
                                    Instance authenticating_instance = null;
                                    await Task.WhenAll(Parent.InstanceSource.InstanceList.Select(instance =>
                                    {
                                        var authorization_task = new Task(
                                            () =>
                                            {
                                                var e = new RequestAuthorizationEventArgs("config") { SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly };
                                                Parent.Instance_RequestAuthorization(instance, e);
                                                if (e.AccessToken != null)
                                                    lock (authenticating_instance_lock)
                                                        authenticating_instance = instance;
                                            },
                                            Window.Abort.Token,
                                            TaskCreationOptions.LongRunning);
                                        authorization_task.Start();
                                        return authorization_task;
                                    }));

                                    if (authenticating_instance != null)
                                    {
                                        // Save found instance.
                                        instance_source_distributed.AuthenticatingInstance = authenticating_instance;
                                        instance_source_distributed.ConnectingInstance = authenticating_instance;

                                        // Go to (instance and) profile selection page.
                                        Parent.CurrentPage = Parent.RecentConfigurationSelectPage;
                                    }
                                    else
                                        Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                }
                                else if (Parent.InstanceSource is FederatedInstanceSource instance_source_federated)
                                {
                                    // Trigger initial authorization request.
                                    await Parent.TriggerAuthorizationAsync(instance_source_federated.AuthenticatingInstance);

                                    Parent.CurrentPage = Parent.RecentConfigurationSelectPage;
                                }
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        instance_source_type =>
                            instance_source_type is InstanceSourceType &&
                            Parent.InstanceSources != null &&
                            Parent.InstanceSources[(int)instance_source_type] != null);

                    // Setup canExecute refreshing.
                    // Note: Parent.InstanceSources is pseudo-static. We don't need to monitor it for changes.
                }

                return _select_instance_source;
            }
        }
        private DelegateCommand<InstanceSourceType?> _select_instance_source;

        /// <summary>
        /// Select custom instance source
        /// </summary>
        public DelegateCommand SelectCustomInstance
        {
            get
            {
                if (_select_custom_instance == null)
                {
                    _select_custom_instance = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Assume the custom instance would otherwise be a part of "Institute Access" source.
                                Parent.InstanceSourceType = InstanceSourceType.InstituteAccess;

                                Parent.CurrentPage = Parent.CustomInstancePage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        });
                }

                return _select_custom_instance;
            }
        }
        private DelegateCommand _select_custom_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance source selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceSourceSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Members

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.RecentConfigurationSelectPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return Parent.StartingPage != this;
        }

        #endregion
    }
}