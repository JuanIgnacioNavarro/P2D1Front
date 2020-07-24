﻿using CookTime.FileHelpers;
using CookTime.Models;
using CookTime.Services;
using GalaSoft.MvvmLight.Command;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace CookTime.ViewModels
{
    public class MyMenuViewModel : BaseViewModel
    {
        #region ATTRIBUTES

        //RECEIPES SHOWN IN THE VIEW

        
        private ObservableCollection<RecipeItemViewModel> recipes; //Recipe collection, this collection has to change if the 
                                                                   //user touches the filter buttons

        private List<Recipe> recipesList; //list of teh recipes loaded from the server

        //ACTIVITY INDICATOR
        private bool isRefreshing; 

        //VALUE
        private int followers;

        private int following;

        //TEXT
        private string name;

        private string email;

        private string age;

        //OTHER
        private User loggedUser;

        private MediaFile file;

        private ImageSource addImageSource;

        private Byte[] ImageByteArray;

        #endregion


        #region PROPERTIES

        //VALUE
        public int Followers
        {
            get { return this.followers; }
            set { SetValue(ref this.followers, value); }
        }

        public int Following
        {
            get { return this.following; }
            set { SetValue(ref this.following, value); }
        }

        //TEXT
        public string Name 
        {
            get { return name; }
            set { SetValue(ref this.name, value); }
        }

        public string Email 
        {
            get { return  this.email; }
            set { SetValue(ref this.email, value); }
        }

        public string Age 
        {
            get { return this.age; }
            set { SetValue(ref this.age, value); }
        }

        //OBSERVABLE COLLECTION
        public ObservableCollection<RecipeItemViewModel> Recipes
        {
            get { return this.recipes; }
            set { SetValue(ref this.recipes, value); }
        }

        //ACTIVITY INDICATOR
        public bool IsRefreshing 
        { 
            get { return this.isRefreshing; }
            set { SetValue(ref this.isRefreshing, value); }
        }

        //COMMANDS
        public ICommand CheffQueryCommand { get { return new RelayCommand(CheffQuery); } }

        public ICommand SortCommand { get { return new Command<string>(SortList); } }
    
        public ICommand RefreshCommand { get { return new RelayCommand(RefreshAux); } }
  
        public ICommand ChangePictureCommand { get { return new RelayCommand(ChangePicture); } }

        //IMAGE SOURCE
        public ImageSource AddImageSource
        {
            get { return this.addImageSource; }
            set { SetValue(ref this.addImageSource, value); }
        }
        #endregion


        #region CONSTRUCTOR

        public MyMenuViewModel()
        {
            this.loggedUser = TabbedHomeViewModel.getUserInstance();
            init();
            this.SortList("0");
        }
        #endregion


        #region METHODS

        private void init()
        {
            this.Name = loggedUser.Name;
            this.Email = loggedUser.Email;
            this.Age = loggedUser.Age;
            this.Followers = loggedUser.Followers.Length;
            this.Following = loggedUser.UsersFollowing.Length;

            if (loggedUser.ProfilePic == null)
            {
                this.AddImageSource = "SignUpIcon";
                return;
            }

            this.AddImageSource = loggedUser.UserImage;
        }

     
        private void CheffQuery()
        {
            //ToDo: Command that creates the cheff query navigation page
        }

        /*
         * Helps the refresh command
         */
        private void RefreshAux()
        {
            SortList("0");
        }

        /*
        * Loads the recipe saved in the server in chronological order
        */
        private async void SortList(string sortType)
        {
            this.IsRefreshing = true;
            //Check internet connection
            var connection = await ApiService.CheckConnection();

            if (!connection.IsSuccess)
            {
                this.IsRefreshing = false;
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    connection.Message,
                    "Accept");
                return;
            }

            //Creates the url needed to getthe information
            var url = "/recipes/" + this.Email + "?sortingType=" + sortType;

            //Asks the server for the list of recipes
            var response = await ApiService.GetList<Recipe>(
                "http://localhost:8080/CookTime.BackEnd",
                "/api",
                url);

            if (!response.IsSuccess)
            {
                this.IsRefreshing = false;
                await Application.Current.MainPage.DisplayAlert( //if something goes wrong the page displays a message
                    "Welcome to CookTime",
                    "Add new recipes to have a complete MyMenu",
                    "Accept");
                return;
            }

            //Copies the list loaded from the server
            
            this.recipesList = (List<Recipe>)response.Result;

            if (this.recipesList==null)
            {
                return;
            }

            await LoadUserProfilePic();

            ChangeStringSpaces();

            //Creates observable collection
            this.Recipes = new ObservableCollection<RecipeItemViewModel>(this.ToRecipeItemViewModel());
            this.IsRefreshing = false;
        }

        /*
         * Changes the recipe list into a recipe item view model 
         */
        private IEnumerable<RecipeItemViewModel> ToRecipeItemViewModel()
        {
            return this.recipesList.Select(r => new RecipeItemViewModel
            {
                Name = r.Name,
                Author = r.Author,
                Type = r.Type,
                Portions = r.Portions,
                CookingSpan = r.CookingSpan,
                EatingTime = r.EatingTime,
                Tags = r.Tags,
                Image = r.Image,
                Ingredients = r.Ingredients,
                Steps = r.Steps,
                Comments = r.Comments,
                Likers = r.Likers,
                Price = r.Price,
                Difficulty = r.Difficulty,
                Punctuation = r.Punctuation,
                Shares = r.Shares,
                RecipeImage = r.RecipeImage,
                UserImage = r.UserImage
            });
        }

        private async void ChangePicture()
        {
            await CrossMedia.Current.Initialize();

            string source = await Application.Current.MainPage.DisplayActionSheet("Image source", "Cancel", null, "Take picture", "Open Gallery");

            if (string.IsNullOrEmpty(source) || source.Equals("Cancel"))
            {
                this.file = null;
                return;
            }

            if (source.Equals("Take picture"))
            {
                this.file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "test.jpg",
                    PhotoSize = PhotoSize.Small,
                }
                );
            }

            else
            {
                this.file = await CrossMedia.Current.PickPhotoAsync();
            }

            if (this.file != null)
            {
                this.AddImageSource = ImageSource.FromStream(() =>
                {
                    var stream = this.file.GetStream();
                    return stream;
                });

                this.ImageByteArray = FileHelper.ReadFully(this.file.GetStream());

            }
            else
            {
                return;
            }

            string arrayConverted = Convert.ToBase64String(this.ImageByteArray);

            var userImage = new UserImage
            {
                User = this.Email,
                Image = arrayConverted
            };

            var responseImage = await ApiService.Put<UserImage>(
                "http://localhost:8080/CookTime.BackEnd",
                "/api",
                "/users/profilePic",
                userImage,
                false);

            if (!responseImage.IsSuccess)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    responseImage.Message,
                    "Accept");
                return;
            }
        }

        public void ChangeStringSpaces()
        {
            foreach (Recipe recipe in this.recipesList)
            {
                recipe.Name = ReadStringConverter.ChangeGetString(recipe.Name);
                recipe.CookingSpan = ReadStringConverter.ChangeGetString(recipe.CookingSpan);
                recipe.EatingTime = ReadStringConverter.ChangeGetString(recipe.EatingTime);
                recipe.Ingredients = ReadStringConverter.ChangeGetString(recipe.Ingredients);
                recipe.Steps = ReadStringConverter.ChangeGetString(recipe.Steps);
                recipe.Tags = ReadStringConverter.ChangeGetString(recipe.Tags);
            }
        }

        private async Task<Response> LoadUserProfilePic()
        {
            foreach (Recipe recipe in this.recipesList)
            {
                string controller = "/users/" + recipe.Author; //Asking for the account information
                Response checkEmail = await ApiService.Get<User>( //Tries to get the account information
                    "http://localhost:8080/CookTime.BackEnd",
                    "/api",
                    controller);
                User userX = (User)checkEmail.Result;

                recipe.UserImage = userX.UserImage;

            }
            return null;
        }
        #endregion
    }
}
