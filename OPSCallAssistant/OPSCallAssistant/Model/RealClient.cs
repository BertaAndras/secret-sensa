﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using OPSSDK;
using OPSSDKCommon.Model;
using OPSSDKCommon.Model.Extension;
using Ozeki.VoIP;


namespace OPSCallAssistant.Model
{
    class RealClient : IClient
    {
        public event EventHandler<VoIPEventArgs<LoginResult>> LoginCompleted;
        public event EventHandler<VoIPEventArgs<ISession>> SessionCompleted;
        public event EventHandler<VoIPEventArgs<ISession>> SessionCreated;
        public event EventHandler<VoIPEventArgs<ErrorInfo>> ErrorOccurred;
        public event EventHandler<VoIPEventArgs<List<PhoneBookItem>>> PhoneBookChanged;
        public UserInfo User { get; private set; }
        public bool IsLoggedIn { get; private set; }

        OpsClient ops_client;

        public void Login(UserInfo user)
        {
            ops_client = new OpsClient();
            ops_client.ErrorOccurred += OPSClientOnErrorOccurred;
            ops_client.PhoneBookItemsChanged += OnPhoneBookChanged;
            User = user;
            ops_client.LoginAsync(user.ServerAddress, user.Username, user.Password, OnLoginCompleted);
        }

        public void Logout()
        {
            IsLoggedIn = false;

            ops_client.Close();
            ops_client.PhoneBookItemsChanged -= OnPhoneBookChanged;
            ops_client.ErrorOccurred -= OPSClientOnErrorOccurred;
            ops_client.SessionCreated -= OPSClientOnSessionCreated;
            ops_client.SessionCompleted -= OPSClientOnSessionCompleted;
        }

        public IAPIExtension GetAPIExtension(string extension_name)
        {
            return ops_client.GetAPIExtension(extension_name);
        }

        public void GetAPIExtensionAsync(string extension_name, Action<IAPIExtension> completed)
        {
            ops_client.GetAPIExtensionAsync(extension_name, completed);
        }

        public List<ExtensionInfo> ExtensionInfos
        {
            get { return ops_client.GetExtensionInfos(); }
        }

        public void GetExtensionInfosAsync(Action<List<ExtensionInfo>> completed)
        {
            ops_client.GetExtensionInfosAsync(completed);
        }

        void OnLoginCompleted(bool success)
        {
            IsLoggedIn = success;

            if (success)
            {
                ops_client.SessionCreated += OPSClientOnSessionCreated;
                ops_client.SessionCompleted += OPSClientOnSessionCompleted;
            }
            else
            {
                User = null;
            }

            var handler = LoginCompleted;
            if (handler != null) handler(this, IsLoggedIn ? new VoIPEventArgs<LoginResult>(LoginResult.Success) : new VoIPEventArgs<LoginResult>(LoginResult.UsernameOrPassword));
        }

        public void GetPhoneBook()
        {
            ops_client.GetPhoneBookAsync(OnGotPhoneBook);
        }


        private void OnPhoneBookChanged(object sender, EventArgs e)
        {
            GetPhoneBook();
        }

        private void OnGotPhoneBook(List<PhoneBookItem> phoneBookItems)
        {
            var handler = PhoneBookChanged;
            if (handler != null)
            {
                handler(this, new VoIPEventArgs<List<PhoneBookItem>>(phoneBookItems));
            }
        }

        void OPSClientOnSessionCompleted(object sender, VoIPEventArgs<ISession> vo_ip_event_args)
        {
            Debug.WriteLine("{0} - Call completed", vo_ip_event_args.Item.SessionID);

            var handler = SessionCompleted;
            if (handler != null) handler(this, vo_ip_event_args);
        }

        void OPSClientOnSessionCreated(object sender, VoIPEventArgs<ISession> vo_ip_event_args)
        {
            //Debug.WriteLine("{0} - Call created", vo_ip_event_args.Item.SessionID);

            //vo_ip_event_args.Item.SessionStateChanged += ((o, args) =>
            //{
            //	Debug.WriteLine("{0} - Call state changed: {1}", vo_ip_event_args.Item.SessionID, vo_ip_event_args.Item.State);
            //});

            var handler = SessionCreated;
            if (handler != null) handler(this, vo_ip_event_args);
        }

        static LoginResult ErrorTypeToLoginResult(ErrorType error_type)
        {
            switch (error_type)
            {
                case ErrorType.ConnectionFailure:
                    return LoginResult.ConnectionFailure;
                case ErrorType.VersionMismatch:
                    return LoginResult.VersionMismatch;
                default:
                    return LoginResult.UnkownError;
            }
        }

        void OPSClientOnErrorOccurred(object sender, ErrorInfo error_info)
        {
            Debug.WriteLine("{0}", error_info.Message);

            if (!IsLoggedIn)
            {
                var handler = LoginCompleted;
                if (handler != null) handler(this, new VoIPEventArgs<LoginResult>(ErrorTypeToLoginResult(error_info.Type)));
            }

            var error_handler = ErrorOccurred;
            if (error_handler != null) error_handler(this, new VoIPEventArgs<ErrorInfo>(error_info));
        }
    }
}
