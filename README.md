# ScutLogin
华南理工大学（大学城校区）宿舍WLAN scut-student自动登录程序。

##项目介绍
* **ScutLogin.Shared** 这是登录状态的识别、登录、注销等核心功能的实现，与平台无关。
* **ScutLogin.Droid** 这是一个Xamarin.Android项目，实现Android的用户界面，并依赖Android的系统广播实现连接时自动登录。
