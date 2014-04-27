window.onload = function() {
	enchant();

	var game = new Game(320, 320);
	game.preload('chara1.png');
	game.onload = function() {
		// ここに処理を書いていきます。
		var bear = new Sprite(32, 32);
		bear.image = game.assets['chara1.png'];
		
		game.rootScene.addChild(bear);
	};
	game.start();
};