window.onload = function() {
	enchant();

	var game = new Game(320, 320);
	game.onload = function() {
		// ここに処理を書いていきます。
		
		var pad = new Pad();
		pad.x = 0;
		pad.y = 220;
		game.rootScene.addChild(pad);
	};
	game.start();
};