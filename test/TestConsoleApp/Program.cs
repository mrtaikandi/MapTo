using VM = TestConsoleApp.ViewModels;
using Data = TestConsoleApp.Data.Models;

var userViewModel = VM.User.From(new Data.User());
var userViewModel2 = VM.UserViewModel.From(new Data.User()); 