using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Beamable;
using Beamable.Stats;
using Beamable.AccountManagement;
using Beamable.Common.Api.Auth;
using TMPro;
using Beamable.Common;

namespace IEdgeGames {

    public class Auth : SingletonBehaviour<Auth> {

        // =========================================================================================================

        [Header("Login")]
        [SerializeField] private TMP_InputField m_Username;
        [SerializeField] private TMP_InputField m_Password;

        [Header("Buttons")]
        [SerializeField] private Button m_OpenPanel;
        [SerializeField] private Button m_ClosePanel;
        [SerializeField] private Button m_Login;
        [SerializeField] private Button m_CreateUser;
        [SerializeField] private Button m_Cancel;

        //[SerializeField] private StatBehaviour m_DisplayNameStat;
        //[SerializeField] private StatBehaviour m_SubTextStat;

        private static BeamContext m_BeamContext;
        //private User m_User;
        //private string m_Alias;

        // =========================================================================================================

        /// <summary>
        /// 
        /// </summary>
        public static User User { get; private set; }//Instance ? Instance.m_User : null;

        /// <summary>
        /// 
        /// </summary>
        //public static string Alias => Instance? Instance.m_Alias : "";

        // =========================================================================================================

        /*/// <summary>
        /// 
        /// </summary>
        /// <param name="onComplete"></param>
        public static void CreateUser(Action<User, string> onComplete = null) {
            if (Instance)
                Instance.CreateUserInternal(Instance.m_Username.text, Instance.m_Password.text, onComplete);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onComplete"></param>
        public static void LoginUser(Action<User, string> onComplete = null) {
            if (Instance)
                Instance.LoginUserInternal(Instance.m_Username.text, Instance.m_Password.text, onComplete);
        }*/

        // =========================================================================================================

        private GameObject _gob;
        private AccountManagementSignals _signaler;
        //private MockBeamableApi _engine;
        private User _engineUser;
        private Promise<Unit> _pendingPromise;
        private LoadingArg _loadingArg;


        new async void Start() {
            base.Start();


            m_BeamContext = BeamContext.Default;

            //await m_BeamContext.Api.AuthService.RemoveAllDeviceIds();

            //m_BeamContext.OnUserLoggedIn += OnAuth;

            await m_BeamContext.OnReady;

            m_BeamContext.OnUserLoggedIn += OnAuth;

            //await m_BeamContext.Api.AuthService.RemoveAllDeviceIds();
            m_BeamContext.Api.ClearDeviceUsers();


            /*m_Username.text = "tlordmarshall@gmail.com";
            m_Password.text = "5F~j3T2svL?4W.kt";
            LoginUser();*/

            //Debug.LogWarning(BeamContext.Default.AuthorizedUser.Value.email);
            //Debug.LogWarning();

            //Init();
        }


        public void Init() {
            /*_engineUser = new User();
            _engine = new MockBeamableApi();
            _engine.User = _engineUser;
            _engine.GetDeviceUsersDelegate = () => Promise<ISet<UserBundle>>.Successful(new HashSet<UserBundle>());

            API.Instance = Promise<IBeamableAPI>.Successful(_engine);*/

            //_gob = new GameObject();
            _signaler = GetComponent<AccountManagementSignals>();
            _signaler.Login("tlordmarshall@gmail.com", "5F~j3T2svL?4W.kt");
            //_signaler.PrepareForTesting(_gob, arg => _loadingArg = arg);

            //_pendingPromise = new Promise<Unit>();

            //StartCoroutine(SignalsALoadingEvent());
        }



        public IEnumerator SignalsALoadingEvent() {

            _signaler.Loading.AddListener(arg => _pendingPromise.CompleteSuccess(PromiseBase.Unit));

            /*_engine.MockAuthService.IsEmailAvailableDelegate = email => Promise<bool>.Successful(true);
            _engine.MockAuthService.RegisterDbCredentialsDelegate =
               (email, password) => Promise<User>.Successful(null);*/

            //_signaler.UserLoggedIn = new UserEvent();
            _signaler.UserLoggedIn.AddListener(u => {

                Debug.LogWarning(u.email);
            });
            _signaler.Login("tlordmarshall@gmail.com", "5F~j3T2svL?4W.kt");

            //yield return _pendingPromise.AsYield();
            yield return null;
            //Assert.AreEqual(true, _pendingPromise.IsCompleted);
            //Assert.AreEqual(true, _loadingArg.Promise.IsCompleted);
        }





        public void OnAuth(User user) {
            User = user;

            //foreach (var x in user.scopes)
            Debug.LogWarning(user.email);
        }

        void OnAuthError(string error) {
            Debug.LogWarning(error);
        }

        public void ForgetUser(UserBundle reference) {
            API.Instance.Then(de => {
                if (reference.User.id == de.User.id) {
                    throw new Exception("Cannot forget current user");
                }

                de.RemoveDeviceUser(reference.Token);
            });
        }

        void CreateUser() {
            m_BeamContext.Api.AuthService.RegisterDBCredentials(m_Username.text, m_Password.text)
                //.Then(OnAuth)
                .Error(error => OnAuthError(error.Message)
            );
        }

        /*void LoginToken() {
            m_BeamContext.Api.AuthService.LoginRefreshToken(m_BeamContext.AccessToken.RefreshToken)
                .Then(token => Debug.LogWarning("asdasdadasdasd"))
                .Error(error => OnAuthError(error.Message)
            );
        }*/

        void LoginUser() {
            m_BeamContext.Api.AuthService.Login(m_Username.text, m_Password.text)
			    .Then(token => OnAuth(m_BeamContext.Api.User))
			    .Error(error => OnAuthError(error.Message)
            );
		}
	}
}
