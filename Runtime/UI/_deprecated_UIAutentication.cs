using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Axion.Net;
using TMPro;
using Ui.module.manager;
namespace IEdgeGames {

    public class _deprecated_UIAutentication : UIModule<_deprecated_UIAutentication> {

        //public TMP_InputField inputNick;
        public TMP_InputField inputEmail;
        public TMP_InputField inputPassword;
        AxionNetWS _ws;
        //public GameObject panelGameMode;
        //public GameObject panelAutentication;
        //public GameObject panelAceptedConditionsAndEula;
        public GameObject loading;
        public GameObject btnlogin;
        //public TextMeshProUGUI statusConection;
        public TextMeshProUGUI formAlert;
        MenuManager menuManager;
        [Header("Development")]
        [SerializeField] private bool m_ShowCloseButton = true;
        [SerializeField] private Button m_CloseButton;

        private bool emptyForm;

        void Start() {
             menuManager = GameObject.FindObjectOfType<MenuManager>();
            m_CloseButton.gameObject.SetActive(m_ShowCloseButton);
            m_CloseButton.onClick.AddListener(() => {
                //_deprecated_UIMainMenu.Active = true;
            });

            _ws = FindObjectOfType<AxionNetWS>();

            _ws.OnAuthError += ((title, description) => {
                loading.SetActive(false);
                 btnlogin.SetActive(true);
                Debug.Log("titleError " + title);
                Debug.Log("descriptionError " + description);

            });

            if (Debug.isDebugBuild) {
                inputEmail.text = "will@2axion.com";
                inputPassword.text = "Test123!!";
            }
        }

        //user account Testing backend = user : will@2axion.com password: Test123!!;
        public void AuthenticateLogin() {
            //check if the input fields are not empty and check the email if it is valid 
            isFormValidate();

            //If the form is ok, proceed to show the load and hide the login button 
            if(!emptyForm){
                btnlogin.SetActive(false);
                loading.SetActive(true);
                 formAlert.gameObject.SetActive(false);

            //## Authenticate Login   
                _ws.Authenticate("axionfps", inputEmail.text.ToString(), inputPassword.text.ToString());
                // response if the user is successfully authenticated 
                _ws.OnAuthSuccess += () => {
                     menuManager.autenticationToMainPanels();
                    loading.SetActive(false);
                    // activation panel of game mode and show connection status at the same time as the authentication panel 
                    //panelGameMode.SetActive(true);
                    //statusConection.text = "Connected";
                    //panelAutentication.SetActive(false);

                    //Active = false; // no se necesita, este panel se desactiva automaticamente al abrir un nuevo modulo (mientras no sea persistente)
                    //_deprecated_UIMainMenu.Active = true;
                };
                _ws.OnUserInfo += (userinfo) => {
                    // muestra name del usuario
                    Debug.Log(userinfo.accountId.ToString());

                };
            }
               
        }

        //create account function 
        public void AuthenticateCreate(){
            Application.OpenURL("http://unity3d.com/");
        }

        /*// Hide terms and conditions when accepted 
        public void AceptedConditionsAndEula(){
            panelAutentication.SetActive(true);
            panelAceptedConditionsAndEula.SetActive(false);         
        }*/

        //If the inputs are empty 
        bool isEmptyAndShowAlert(string input, string name){
            if(input.Equals("")){
                emptyForm=true;
                formAlert.gameObject.SetActive(true);
                formAlert.text=name+" is required";
                    
            }else{
                 emptyForm=false;
            }
                return emptyForm;
        }

         //If the inputs are empty or the email is not valid, the emptyform variable is turned on 
        void isFormValidate(){
            if(isEmptyAndShowAlert(inputEmail.text,"E-mail address")){
            }
            else if( !isEmailValidator(inputEmail.text)){
                formAlert.gameObject.SetActive(true);
                formAlert.text="Email address is not valid";
            }else{ 
                isEmptyAndShowAlert(inputPassword.text.ToString(),"Password");
            }
        }

        //if the email is valid ;
        bool isEmailValidator( string email){
            var regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            emptyForm=!regex.IsMatch(email);

            return regex.IsMatch(email);
            }
        }
}
