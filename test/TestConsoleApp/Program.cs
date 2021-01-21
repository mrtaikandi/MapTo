using TestConsoleApp.ViewModels;
using VM = TestConsoleApp.ViewModels;
using Data = TestConsoleApp.Data.Models;

var userViewModel = VM.User.From(new Data.User());
var userViewModel2 = new Data.User().ToUserViewModel(); 