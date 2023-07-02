Moo.Public
=

A selection of some quick-and-dirtyish scambaiting things that I've decided to make public.
The ones that spawn a window use Avalonia UI and Moo.Notifications is a console app.

### Moo.CustomFakeNotification
A semi-transparent window that appears in the bottom-right corner of the screen somewhat reminiscent of fake AVs from the Windows XP days. It does a fake scan that just goes through the names of running processes and flashes "Hackers detected!". The majority of the text is hard-coded in the main window view model, but the phone number can be changed to whatever line of text you want by editing number.txt. This window can only be closed by pressing the left Alt key 3 times in a row (you may need to spam it) or by killing the process. Alt-F4 will not work.

!["Custom fake notification"](/screenshots/youcomputerindanger.png)

### Moo.Notifications
This spawns notifications appearing to be coming from Google Chrome that can be customized via the directory of images and the Notifications.json file. The images folder and Notifications.json must be in the same folder as the program. Notifications automatically respawn when closed and go to the URL specified in the JSON file when clicked. After a random delay, a new notification is chosen and spawns. Only killing the process or disabling notifications for it in Windows' settings will stop new ones from spawning. 

!["Notifications"](/screenshots/moofriend.png)

### Moo.TeamViewer
Displays a window that looks like the one that TeamViewer displays when someone connects. Useful if you want to get a scammer to argue with you about someone else being connected to your VM. The text is hardcoded in the main window AXAML file.

!["Fake TeamViewer"](/screenshots/teamviewer.png)

### Moo.Update
A fake Windows 10 update screen that never finishes - in fact, it will roll back when it gets to 99%. Since this is just a window, scammers will see it (unlike the real update screen). It aggressively keeps itself on top and prevents any alt-tab switching. The speed at which it progresses can be configured in WindowSettings.json, and you can specify a different speed for when it gets above 90%. It progresses a random percentage between 1 and the interval you specify a random number of seconds between two values specified. There's some trickery a scammer could do to log you out (and close the process) with Ctrl-Alt-Del, but the only other way to close it is to either kill the process remotely or press left Alt 3 times in a row (spam it if necessary).

!["Fake Update"](/screenshots/update.png)