NavBall Texture Changer

This mod was originally written by forum user @xEvilReeperx.  It was taken over by forum user @TheRagingIrishman.

Due to a total lack of updates since 8/6/2017, I've adopted it.

Thanks to forum user @therealcrow999 for his assistance with the emissives
Thanks to @Stone_Blue for his work in collecting all the available textures in one place

Major changes in this version
	External/visible
		Added UI to select navball texture in-game without any file editing
		Now allows separate skin for IVA
	Internal
		Removed usage of persistence to save config
		Added methods to save and load config
		Moved config file into PluginData folder
		Added library of NavBall textures with config files 
		Stopped using the GameDatabase to load the config and texture

Dependencies
        ClickThroughBlocker
        ToolbarController
        SpaceTuxLibrary 

Note
	Emissives are only used in IVA mode

Usage
	Click the Navball icon to open the window

	Window will be in two parts.  Left half shows the available textures, right half shows the currently selected texture
	Toggles at the bottom
		Only w/ emissives		Only shows the textures with available emissives
		Advanced				Enables advanced mode, only available when in IVA.  This will allow you to fine-tune the emissive values 

	Buttons at bottom of main window:
		Test			Apply the currently selected texture, but don't save it.  You must test a texture before saving it
		Save			Save the currently selected (and applied) texture.
		Reset to Stock	Reset navball texture to stock
		Close			Close the window

	Buttons at bottom of Advanced section
		Apply Emissive Changes		Applies the changes to the current navball.  Must be applied before saving
		Save Emissive Changes		Save the changes.  Note that the changes will be lost if you change textures
		Reset						Reset to original settings