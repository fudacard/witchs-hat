window.onload = function() {
	enchant();

	var game = new Game(320, 320);
	game.preload('chara1.png');
	game.onload = function() {
		// ここに処理を書いていきます。
		var sx = 3;
		var bear = new Sprite(32, 32);
		bear.image = game.assets['chara1.png'];
		bear.frame = [1, 1, 2, 2];
		
		game.rootScene.onenterframe = function(e) {
			bear.x += sx;
			if (bear.x < 0 || bear.x + bear.width > game.width) {
				sx *= -1;
				bear.scaleX *= -1;
			}
		};
		
		game.rootScene.addChild(bear);
	};
	game.start();
};