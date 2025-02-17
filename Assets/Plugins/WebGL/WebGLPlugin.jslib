mergeInto(LibraryManager.library, {
    InitializeWebGL: function() {
        // Basic WebGL initialization
        console.log("Initializing WebGL plugin...");
        
        // Return success
        return 1;
    },
}); 