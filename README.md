SpaceRace is a mod for Juno: New Origins.  It allows players to assume the roles of space program administrators in the United States.  Can you repeat history and land a person on the Moon before 1970?  The career and craft construction systems from the game are completely overhauled. 


---------- Key Features ----------

* Play a custom campaign starting in 1950 on Earth using period appropriate hardware.

* Decide which engines and capsules to develop.  You aren't limited to historical models.

* Manage time and plan ahead.  Parts and stages must be developed.  Craft must be integrated and rolled to the pad.  

* Select the right contractor to develop and produce each piece of hardware.  Some are cheaper. Some are faster. All are overly optimistic.  All will charge you cost plus.

* Weigh the benefits of developing new hardware versus revamping or reusing what you already have.

* Plan around engine failures.  The more you fly with similar technology, the more reliable it becomes.

* Choose guidance and communications capabilities to match your mission profile.

* Stay within your budget.  Most contracts provide continuous funding until completed.  Funding rates will ebb and flow as government priorities shift.

* React to historical events or drive them.


---------- Installation Instructions ----------

* Download and install the mod Harmony from https://www.simplerockets.com/Mods/View/234638/Juno-Harmony

* Install the Solar System from https://www.simplerockets.com/PlanetarySystems/View/XiaL6J/Solar-System-2-1 unless you already have another solar system installed with a detailed Cape Canaveral like RSS.

* Download SpaceRace.mod-2 and Career.zip from the releases =>

* Copy SpaceRace.mod-2 to you mods folder, usually C:/Users/{yourusername}/AppData/LocalLow/Jundroo/SimpleRockets2/Mods

* Unzip the Career.zip file into your career folder, usually C:/Users/{yourusername}/AppData/LocalLow/Jundroo/SimpleRockets2/Career.  Your Career folder should now have 3 folders in it: Default, Hybrid and SpaceRace.

* Start the game and enable the Harmony and SpaceRace mods (in that order).

* Start a new game. Select the solar system from the sandbox options.  Then select the SpaceRace career from the career options.


---------- Q&A ----------

Q: Why should I play this instead of the excellent RP-1 mod for Kerbal Space Program?

A: You shouldn't!  RP-1 is my favorite gaming experience ever, and you should check it out too.  Having said that, Juno is a different beast than KSP.  In the area of customizable parts, loading times, location targeting, and the ease of writing flight programs, I think it has something to add.  In addition, SpaceRace uses its focus on the United States to give a more detailed reflection of the nature of the US space program, especially with historical events and the role of contractors.

Q: Why are my rockets falling over?

A: Juno doesn't always keep precise track of grounded craft when warping or loading.  If they shift or rotate slightly, they can topple over.  I included some simple launchpad assemblies to help with stability.  I encourage you to build your own stability-enhancing assemblies in a sandbox (tinker the price to 0), and then use them without guilt in the career.

Q: Why are my engines failing so much and what can I do about it?

A: Rockets were remarkably unreliable in the 1950s.  As you fly an engine or similar engine more, failure rates will drop.  Some tech tree items can help too.  In the meantime, consider waiting to separate from the launchpad until the engines have successfully ignited.  If any fail, you can recover the craft for repairs.  You can try shutting down failed engines in flight, but they might explode anyway.  Include escape motors for your capsules!

Q: How do I hit targets precisely?

A: Flight programs are your friend.  Write one and simulate and tweak it until you get the desired results.  Even so, I find the highest-level targeting missions really hard, but they are based on the actual accuracy achieved by missile engineers in the 1950s.  Those folks were really smart.

Q: How do I get to orbit with a 20 ton craft?

A: The same way that US engineers did in 1958.  Use unguided avionics on your top stage to save mass. An electric motor can spin up the stage before separation to keep it stable(ish).

Q: How do I get crew into the capsule on top of my craft.

A: Include a crew tower (they're free and adjustable) in the craft design or use grappling hooks.

Q: Why are my crew capsules unstable on reentry?

A: In real life, the blunt top of the capsule creates a vacuum which pulls the top of the capsule behind the base.  Juno does not appear to model this effect on capsule parts (it seems to on fuselages and fairings).  Instead, try coning off the top of the capsule with a dome parachute or a pointy tank.  This is the exact opposite of how physics works, but its an easy enough workaround for gameplay purposes.

Q: Why are development delays so unpredictable and potentially devastating?

A: Long development delays are a persistent feature of space programs.  It's part of the reason the military had 3-4 missile programs going at once.  They didn't know which ones would finish late or not at all.  The mod models this approach specifically in the ICBM development contracts.

Q: The program manager is calculating something wrong. How do I fix it?

A: Saving and reloading often helps.  You can also directly edit the program.xml file in the save folder for your gamestate. Juno overwrites save information when the game launches and also before you load a game, so you need to be sneaky to get your changes to stick.

Q: What are the sources of information that went into this mod?

A: Here are my top 4
* Wikipedia (of course)
* Encyclopedia Astronautica: astronautix.com
* Spaceline's excellent information on the historical KSC launch infrastructure: www.spaceline.org/cape-canaveral-launch-sites/
* Donald Sutton's book: A History of Liquid Rocket Engines greatly informed the structure of engine and part development.  

Q: Your code is really sloppy.  Do you even know how to program in Unity?

A: Nope.  I did this because I wanted to learn how.  Feedback and advice are welcome!

Q: I made a cool part that should be integrated into this mod!

A: That's not a question, but I included a pathway for other mods to add parts to SpaceRace.  You need your part modifier data class to implement the ISRPartMod interface.  ProgramManager has commands to register a part mod and tweak the contractors to allow them to develop it.  You can subscribe to an event in SRManager.Instance that fires when ProgramManager loads to execute these commands.  Get in touch if you decide to try this.  I'm happy to provide support.

---------- Known Bugs ----------

The following "bugs" in the Juno code affect this mod

* Ablators do no remove heat when they ablate.  This makes reentry from LEO unrealistically difficult.  (Patched by SpaceRace)

* The method to end the flight scene does not accept the tech tree as a destination (Patched by SpaceRace)

* The UI associates a payload with a contract when both the contract and the payload have null tracking IDs (Patched by SpaceRace)

* Terrain on large planets (Earth) does not generate at a consistent height, causing structures to appear at different heights/depths depending on how the planet is rotated when the flight scene starts.  Future releases should redesign launchpads to be robust to variations in height.

* Launching from a pad also causes visual effects (smoke and light) on nearby pads.  One potential fix it to just remove all effects from pads that are closely packed.

The following bugs are known to me in SpaceRace

* The projects inspector behaves badly.  Some (low priority) fields do not update regularly.  Sometimes new projects do not appear until the scene reloads.  Occasionally, the inability to update the inspector leads other methods to fail.

* If you use the planet studio during a career game, the scripts do not load when you leave the studio.  Reloading your last save fixes this.

* Craft validation will need to be rewritten from scratch.  For now, the validator will not always stop you from integrating a craft that you could not design with your current technology.

---------- Current State of the Mod -----------

* Balance is probably bad.  You may have enough money to land a man on the moon in 1960.  You may be so starved for funds that it is impossible. There is a setting to globally adjust funding.

* There are still lots of bugs (see above).  Reloading the save sometimes helps.

* The launchpads I added are functional and approximately correct in shape, but they aren't great.  

* Most of the payloads are laughably simplistic.  

--------- Road Map -----------

Here's what I have in mind for the next several months.

Phase 1:

* Clean up the code and work through tech debt.  This could take weeks.

* Build better launchpads and payloads, or find someone else willing to do it.

* Flesh out the campaign with additional events and contracts.

* Balance balance balance.

Phase 2:

* Implement ground support infrastructure and flesh out craft recovery.

* Extend the campaign beyond Apollo.  The code for reusable spacecraft is already in place.  Long-term space habitation will require a bunch of code to manage resources on craft that are not currently loaded into view.  Wish me luck.

Phases 3 and 4:

* I'm not going to publicize my plans yet, but the code has been structured to provide a foundation for future expansion that I'm pretty excited about.

THIS MOD AND SOURCE CODE ARE OFFERED AS IS. I OFFER NO ASSURANCES THAT THEY WILL PERFORM AS DESCRIBED HERE.  I AM NOT RESPONSIBLE FOR ANY DAMAGES CAUSED BY THEIR USE. USE AT YOUR OWN RISK.

