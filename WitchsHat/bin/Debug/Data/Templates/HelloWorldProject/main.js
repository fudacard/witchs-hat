window.onload = function() {
	enchant();

	var game = new Game(320, 320);
	game.onload = function() {
		// ここに処理を書いていきます。
		var label = new Label("Hello World!");
		game.rootScene.addChild(label);
	};
	game.start();
};