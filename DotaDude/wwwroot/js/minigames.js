document.addEventListener('DOMContentLoaded', function () {
    // Game data will be loaded here
    let heroQuizData = null;
    let triviaData = null;
    let itemGuesserData = null;

    // Variables for tracking current game state
    let currentGame = '';
    let gameTimerInterval = null;

    // Hero Quiz variables
    let currentQuizQuestion = 0;
    let quizScore = 0;

    // Trivia Game variables
    let currentTriviaQuestion = 0;
    let triviaScore = 0;
    let triviaTimer = 0;

    // Item Guesser variables
    let currentItemQuestion = 0;
    let itemScore = 0;
    let itemTimer = 0;

    // Event listeners for game buttons
    document.querySelectorAll('.dota-btn').forEach(button => {
        if (button.getAttribute('onclick') && button.getAttribute('onclick').includes('startGame')) {
            button.addEventListener('click', function (e) {
                e.preventDefault();
            });
        }
    });

    // Add event listeners for game control buttons if they exist
    const restartButton = document.getElementById('game-restart');
    if (restartButton) {
        restartButton.addEventListener('click', function () {
            restartGame();
        });
    }

    const exitButton = document.getElementById('game-exit');
    if (exitButton) {
        exitButton.addEventListener('click', function () {
            exitGame();
        });
    }

    // Main function to start a game
    window.startGame = function (gameType) {
        currentGame = gameType;

        // Check if game container exists before accessing it
        const gameContainer = document.getElementById('game-container');
        if (!gameContainer) {
            console.error("Could not find game-container element");
            return;
        }

        gameContainer.style.display = 'block';

        // Get all game divs and check if they exist before hiding them
        const heroQuizGame = document.getElementById('hero-quiz-game');
        const triviaGame = document.getElementById('trivia-game');
        const itemGuesserGame = document.getElementById('item-guesser-game');
        const gameTitle = document.getElementById('game-title');

        // Hide all game divs if they exist
        if (heroQuizGame) heroQuizGame.style.display = 'none';
        if (triviaGame) triviaGame.style.display = 'none';
        if (itemGuesserGame) itemGuesserGame.style.display = 'none';

        // Clear any existing intervals
        if (gameTimerInterval) {
            clearInterval(gameTimerInterval);
            gameTimerInterval = null;
        }

        // Initialize the selected game
        switch (gameType) {
            case 'hero-quiz':
                document.getElementById('game-title').textContent = 'Hero Quiz';
                document.getElementById('hero-quiz-game').style.display = 'block';
                initHeroQuiz();
                break;
            case 'trivia-game':
                document.getElementById('game-title').textContent = 'Dota 2 Trivia Challenge';
                document.getElementById('trivia-game').style.display = 'block';
                initTriviaGame();
                break;
            case 'item-guesser-game':
                document.getElementById('game-title').textContent = 'Item Guesser';
                document.getElementById('item-guesser-game').style.display = 'block';
                initItemGuesser();
                break;
        }

        // Scroll to game container
        document.getElementById('game-container').scrollIntoView({ behavior: 'smooth' });
    };

    // Function to restart the current game
    function restartGame() {
        switch (currentGame) {
            case 'hero-quiz':
                initHeroQuiz();
                break;
            case 'trivia-game':
                initTriviaGame();
                break;
            case 'item-guesser-game':
                initItemGuesser();
                break;
        }
    }

    // Function to exit the current game
    function exitGame() {
        if (gameTimerInterval) {
            clearInterval(gameTimerInterval);
            gameTimerInterval = null;
        }

        document.getElementById('game-container').style.display = 'none';
        currentGame = '';
    }

    //==========================
    // HERO QUIZ IMPLEMENTATION
    //==========================

    function initHeroQuiz() {
        // Reset quiz state
        currentQuizQuestion = 0;
        quizScore = 0;

        // Hide results and feedback
        document.getElementById('quiz-results').style.display = 'none';
        document.getElementById('quiz-feedback').style.display = 'none';

        // Reset progress bar and score
        document.getElementById('current-score').textContent = '0';
        document.getElementById('quiz-progress').style.width = '0%';

        // If we haven't loaded the quiz data yet, load it
        if (!heroQuizData) {
            // Here you would normally fetch from an API
            // For now we'll use mock data
            heroQuizData = getHeroQuizMockData();
        }

        // Shuffle the questions
        shuffleArray(heroQuizData);

        // Load the first question
        loadQuizQuestion();
    }

    function loadQuizQuestion() {
        // Check if we've reached the end of the quiz
        if (currentQuizQuestion >= 10) {
            endHeroQuiz();
            return;
        }

        const question = heroQuizData[currentQuizQuestion];

        // Update abilities
        document.getElementById('ability1').textContent = question.abilities[0];
        document.getElementById('ability2').textContent = question.abilities[1];
        document.getElementById('ability3').textContent = question.abilities[2];
        document.getElementById('ability4').textContent = question.abilities[3];

        // Generate options (correct answer + 3 random heroes)
        let options = [question.hero];

        // Add random heroes, making sure there are no duplicates
        while (options.length < 4) {
            const randomHero = heroQuizData[Math.floor(Math.random() * heroQuizData.length)].hero;
            if (!options.includes(randomHero)) {
                options.push(randomHero);
            }
        }

        // Shuffle options
        shuffleArray(options);

        // Update buttons
        const optionButtons = document.querySelectorAll('.option-btn');
        optionButtons.forEach((button, index) => {
            button.textContent = options[index];
            button.className = 'dota-btn w-100 option-btn'; // Reset classes
            button.disabled = false;

            // Add click event
            button.onclick = function () {
                checkQuizAnswer(options[index], question.hero);
            };
        });

        // Update progress
        document.getElementById('current-score').textContent = quizScore;
        document.getElementById('quiz-progress').style.width = `${(currentQuizQuestion / 10) * 100}%`;

        // Hide feedback
        document.getElementById('quiz-feedback').style.display = 'none';
    }

    function checkQuizAnswer(selectedHero, correctHero) {
        // Disable all buttons
        document.querySelectorAll('.option-btn').forEach(btn => {
            btn.disabled = true;
            btn.classList.add('disabled');

            // Highlight correct and incorrect answers
            if (btn.textContent === correctHero) {
                btn.classList.add('correct');
            } else if (btn.textContent === selectedHero && selectedHero !== correctHero) {
                btn.classList.add('incorrect');
            }
        });

        // Check if answer is correct
        const isCorrect = selectedHero === correctHero;

        // Update score
        if (isCorrect) {
            quizScore++;
            document.getElementById('current-score').textContent = quizScore;

            // Show feedback
            const feedback = document.getElementById('quiz-feedback');
            feedback.className = 'alert alert-success';
            feedback.textContent = 'Correct! Well played!';
            feedback.style.display = 'block';
        } else {
            // Show feedback
            const feedback = document.getElementById('quiz-feedback');
            feedback.className = 'alert alert-danger';
            feedback.textContent = `Incorrect! The correct answer was ${correctHero}.`;
            feedback.style.display = 'block';
        }

        // Move to next question after delay
        currentQuizQuestion++;
        setTimeout(loadQuizQuestion, 2000);
    }

    function endHeroQuiz() {
        // Hide question and options
        document.getElementById('quiz-question').style.display = 'none';
        document.getElementById('quiz-options').style.display = 'none';
        document.getElementById('quiz-feedback').style.display = 'none';

        // Show results
        document.getElementById('quiz-results').style.display = 'block';
        document.getElementById('final-score').textContent = quizScore;

        // Set message based on score
        const scoreMessage = document.getElementById('score-message');
        if (quizScore === 10) {
            scoreMessage.textContent = 'Perfect score! You are truly a Dota 2 master!';
        } else if (quizScore >= 7) {
            scoreMessage.textContent = 'Great job! You really know your Dota 2 heroes!';
        } else if (quizScore >= 5) {
            scoreMessage.textContent = 'Not bad! Keep playing to improve your knowledge!';
        } else {
            scoreMessage.textContent = 'Time to brush up on your hero knowledge. Keep practicing!';
        }

        // Update progress bar
        document.getElementById('quiz-progress').style.width = '100%';
    }

    //==========================
    // TRIVIA GAME IMPLEMENTATION
    //==========================

    function initTriviaGame() {
        // Reset game state
        currentTriviaQuestion = 0;
        triviaScore = 0;

        // Hide results and feedback
        document.getElementById('trivia-results').style.display = 'none';
        document.getElementById('trivia-feedback').style.display = 'none';
        document.getElementById('trivia-explanation').style.display = 'none';

        // Reset progress bar and score
        document.getElementById('trivia-score').textContent = '0';
        document.getElementById('trivia-progress').style.width = '0%';

        // Make questions and options visible
        document.getElementById('trivia-question').style.display = 'block';
        document.getElementById('trivia-options').style.display = 'block';

        // If we haven't loaded the trivia data yet, load it
        if (!triviaData) {
            triviaData = getTriviaMockData();
        }

        // Shuffle the questions
        shuffleArray(triviaData);

        // Load the first question
        loadTriviaQuestion();
    }

    function loadTriviaQuestion() {
        // Check if we've reached the end of the quiz
        if (currentTriviaQuestion >= 10) {
            endTriviaGame();
            return;
        }

        // Clear any existing timer
        if (gameTimerInterval) {
            clearInterval(gameTimerInterval);
        }

        const question = triviaData[currentTriviaQuestion];

        // Set the question text
        document.getElementById('trivia-question').textContent = question.question;

        // Reset the explanation and feedback
        document.getElementById('trivia-explanation').style.display = 'none';
        document.getElementById('trivia-feedback').style.display = 'none';

        // Set options
        const options = [...question.options];
        const triviaButtons = document.querySelectorAll('.trivia-option');
        triviaButtons.forEach((button, index) => {
            button.textContent = options[index];
            button.className = 'dota-btn w-100 trivia-option'; // Reset classes
            button.disabled = false;

            // Add click event
            button.onclick = function () {
                checkTriviaAnswer(index, question.correctIndex, question.explanation);
            };
        });

        // Update progress
        document.getElementById('trivia-score').textContent = triviaScore;
        document.getElementById('trivia-progress').style.width = `${(currentTriviaQuestion / 10) * 100}%`;

        // Set and start timer
        triviaTimer = 20;
        document.getElementById('trivia-timer').textContent = triviaTimer;

        gameTimerInterval = setInterval(function () {
            triviaTimer--;
            document.getElementById('trivia-timer').textContent = triviaTimer;

            if (triviaTimer <= 0) {
                clearInterval(gameTimerInterval);
                timeExpiredTrivia(question.correctIndex, question.explanation);
            }
        }, 1000);
    }

    function checkTriviaAnswer(selectedIndex, correctIndex, explanation) {
        // Clear timer
        clearInterval(gameTimerInterval);

        // Disable all buttons
        document.querySelectorAll('.trivia-option').forEach((btn, index) => {
            btn.disabled = true;
            btn.classList.add('disabled');

            // Highlight correct and incorrect answers
            if (index === correctIndex) {
                btn.classList.add('correct');
            } else if (index === selectedIndex && selectedIndex !== correctIndex) {
                btn.classList.add('incorrect');
            }
        });

        // Check if answer is correct
        const isCorrect = selectedIndex === correctIndex;

        // Update score
        if (isCorrect) {
            triviaScore++;
            document.getElementById('trivia-score').textContent = triviaScore;

            // Show feedback
            const feedback = document.getElementById('trivia-feedback');
            feedback.className = 'alert alert-success';
            feedback.textContent = 'Correct! Well done!';
            feedback.style.display = 'block';
        } else {
            // Show feedback
            const feedback = document.getElementById('trivia-feedback');
            feedback.className = 'alert alert-danger';
            feedback.textContent = 'Incorrect! Better luck with the next question.';
            feedback.style.display = 'block';
        }

        // Show explanation
        if (explanation) {
            document.getElementById('trivia-explanation').textContent = explanation;
            document.getElementById('trivia-explanation').style.display = 'block';
        }

        // Move to next question after delay
        currentTriviaQuestion++;
        setTimeout(loadTriviaQuestion, 3000);
    }

    function timeExpiredTrivia(correctIndex, explanation) {
        // Disable all buttons
        document.querySelectorAll('.trivia-option').forEach((btn, index) => {
            btn.disabled = true;
            btn.classList.add('disabled');

            // Highlight correct answer
            if (index === correctIndex) {
                btn.classList.add('correct');
            }
        });

        // Show feedback
        const feedback = document.getElementById('trivia-feedback');
        feedback.className = 'alert alert-warning';
        feedback.textContent = 'Time expired! You need to be faster!';
        feedback.style.display = 'block';

        // Show explanation
        if (explanation) {
            document.getElementById('trivia-explanation').textContent = explanation;
            document.getElementById('trivia-explanation').style.display = 'block';
        }

        // Move to next question after delay
        currentTriviaQuestion++;
        setTimeout(loadTriviaQuestion, 3000);
    }

    function endTriviaGame() {
        // Clear timer
        if (gameTimerInterval) {
            clearInterval(gameTimerInterval);
        }

        // Hide question and options
        document.getElementById('trivia-question').style.display = 'none';
        document.getElementById('trivia-options').style.display = 'none';
        document.getElementById('trivia-feedback').style.display = 'none';
        document.getElementById('trivia-explanation').style.display = 'none';

        // Show results
        document.getElementById('trivia-results').style.display = 'block';
        document.getElementById('trivia-final-score').textContent = triviaScore;

        // Set message based on score
        const scoreMessage = document.getElementById('trivia-score-message');
        if (triviaScore === 10) {
            scoreMessage.textContent = 'Perfect! You are a true Dota 2 lore master!';
        } else if (triviaScore >= 7) {
            scoreMessage.textContent = 'Great job! Your knowledge of Dota 2 is impressive!';
        } else if (triviaScore >= 5) {
            scoreMessage.textContent = 'Not bad! Keep playing to improve your knowledge!';
        } else {
            scoreMessage.textContent = 'Time to brush up on your Dota 2 trivia!';
        }

        // Update progress bar
        document.getElementById('trivia-progress').style.width = '100%';
    }

    //==========================
    // ITEM GUESSER IMPLEMENTATION
    //==========================

    function initItemGuesser() {
        // Reset game state
        currentItemQuestion = 0;
        itemScore = 0;

        // Hide results and feedback
        document.getElementById('item-results').style.display = 'none';
        document.getElementById('item-feedback').style.display = 'none';

        // Reset progress bar and score
        document.getElementById('item-score').textContent = '0';
        document.getElementById('item-progress').style.width = '0%';

        // If we haven't loaded the item data yet, load it
        if (!itemGuesserData) {
            itemGuesserData = getItemGuesserMockData();
        }

        // Shuffle the questions
        shuffleArray(itemGuesserData);

        // Load the first question
        loadItemQuestion();
    }

    function loadItemQuestion() {
        // Check if we've reached the end of the game
        if (currentItemQuestion >= 10) {
            endItemGuesser();
            return;
        }

        // Clear any existing timer
        if (gameTimerInterval) {
            clearInterval(gameTimerInterval);
        }

        const question = itemGuesserData[currentItemQuestion];

        // Set the item description
        document.getElementById('item-description').textContent = question.description;
        document.getElementById('item-cost').textContent = question.cost + ' gold';

        // Set component icons
        const componentList = document.getElementById('component-list');
        componentList.innerHTML = '';

        if (question.components && question.components.length > 0) {
            question.components.forEach(component => {
                const componentImg = document.createElement('img');
                componentImg.src = component.icon;
                componentImg.alt = component.name;
                componentImg.title = component.name;
                componentImg.className = 'component-icon';
                componentList.appendChild(componentImg);
            });
        } else {
            const noComponents = document.createElement('p');
            noComponents.textContent = 'No recipe components';
            componentList.appendChild(noComponents);
        }

        // Generate options (correct answer + 3 random items)
        let optionItems = [question];

        // Add random items, making sure there are no duplicates
        while (optionItems.length < 4) {
            const randomItem = itemGuesserData[Math.floor(Math.random() * itemGuesserData.length)];
            if (!optionItems.includes(randomItem)) {
                optionItems.push(randomItem);
            }
        }

        // Shuffle options
        shuffleArray(optionItems);

        // Update item options
        const itemOptions = document.querySelectorAll('.item-option');
        itemOptions.forEach((option, index) => {
            const img = option.querySelector('img');
            img.src = optionItems[index].icon;
            img.alt = optionItems[index].name;

            option.className = 'item-option'; // Reset classes
            option.onclick = function () {
                checkItemAnswer(optionItems[index].name, question.name);
            };
        });

        // Update progress
        document.getElementById('item-score').textContent = itemScore;
        document.getElementById('item-progress').style.width = `${(currentItemQuestion / 10) * 100}%`;

        // Reset feedback
        document.getElementById('item-feedback').style.display = 'none';

        // Set and start timer
        itemTimer = 15;
        document.getElementById('item-timer').textContent = itemTimer;

        gameTimerInterval = setInterval(function () {
            itemTimer--;
            document.getElementById('item-timer').textContent = itemTimer;

            if (itemTimer <= 0) {
                clearInterval(gameTimerInterval);
                timeExpiredItem(question.name);
            }
        }, 1000);
    }

    function checkItemAnswer(selectedItem, correctItem) {
        // Clear timer
        clearInterval(gameTimerInterval);

        // Disable all options
        document.querySelectorAll('.item-option').forEach(option => {
            const img = option.querySelector('img');

            if (img.alt === correctItem) {
                option.classList.add('correct');
            } else if (img.alt === selectedItem && selectedItem !== correctItem) {
                option.classList.add('incorrect');
            }

            option.onclick = null;
        });

        // Check if answer is correct
        const isCorrect = selectedItem === correctItem;

        // Update score
        if (isCorrect) {
            itemScore++;
            document.getElementById('item-score').textContent = itemScore;

            // Show feedback
            const feedback = document.getElementById('item-feedback');
            feedback.className = 'alert alert-success';
            feedback.textContent = 'Correct! You know your items!';
            feedback.style.display = 'block';
        } else {
            // Show feedback
            const feedback = document.getElementById('item-feedback');
            feedback.className = 'alert alert-danger';
            feedback.textContent = `Wrong! That was ${correctItem}.`;
            feedback.style.display = 'block';
        }

        // Move to next question after delay
        currentItemQuestion++;
        setTimeout(loadItemQuestion, 2000);
    }

    function timeExpiredItem(correctItem) {
        // Highlight correct answer
        document.querySelectorAll('.item-option').forEach(option => {
            const img = option.querySelector('img');

            if (img.alt === correctItem) {
                option.classList.add('correct');
            }

            option.onclick = null;
        });

        // Show feedback
        const feedback = document.getElementById('item-feedback');
        feedback.className = 'alert alert-warning';
        feedback.textContent = 'Time expired! The answer was ' + correctItem;
        feedback.style.display = 'block';

        // Move to next question after delay
        currentItemQuestion++;
        setTimeout(loadItemQuestion, 2000);
    }

    function endItemGuesser() {
        // Clear timer
        if (gameTimerInterval) {
            clearInterval(gameTimerInterval);
        }

        // Hide game elements
        document.getElementById('item-feedback').style.display = 'none';

        // Show results
        document.getElementById('item-results').style.display = 'block';
        document.getElementById('item-final-score').textContent = itemScore;

        // Set message based on score
        const scoreMessage = document.getElementById('item-score-message');
        if (itemScore === 10) {
            scoreMessage.textContent = 'Perfect! You are a true shopkeeper!';
        } else if (itemScore >= 7) {
            scoreMessage.textContent = 'Great job! You really know your items!';
        } else if (itemScore >= 5) {
            scoreMessage.textContent = 'Not bad! Keep browsing the shop to improve your knowledge!';
        } else {
            scoreMessage.textContent = 'Time to study the shop more carefully!';
        }

        // Update progress bar
        document.getElementById('item-progress').style.width = '100%';
    }

    //==========================
    // HELPER FUNCTIONS
    //==========================

    // Function to shuffle array elements
    function shuffleArray(array) {
        for (let i = array.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [array[i], array[j]] = [array[j], array[i]];
        }
    }

    //==========================
    // MOCK DATA
    //==========================

    function getHeroQuizMockData() {
        return [
            {
                hero: 'Axe',
                abilities: [
                    'Berserker\'s Call: Forces nearby enemy units to attack you, granting bonus armor.',
                    'Battle Hunger: Afflicts an enemy unit with hunger, slowing it and causing damage over time.',
                    'Counter Helix: When attacked, you perform a helix counter attack.',
                    'Culling Blade: Executes an enemy unit with low health.'
                ]
            },
            {
                hero: 'Crystal Maiden',
                abilities: [
                    'Crystal Nova: A burst of damaging frost slows enemy movement and attack speed.',
                    'Frostbite: Encases an enemy unit in ice, preventing movement and attack.',
                    'Arcane Aura: Grants bonus mana regeneration to all allies on the map.',
                    'Freezing Field: Surrounds you with explosions of frost that damage and slow nearby enemies.'
                ]
            },
            {
                hero: 'Juggernaut',
                abilities: [
                    'Blade Fury: Spins with your blade, becoming immune to magic and dealing damage to nearby enemies.',
                    'Healing Ward: Summons a ward that heals all nearby allied units.',
                    'Blade Dance: Gives you a chance to deal critical damage on each attack.',
                    'Omnislash: Performs a series of lightning-fast slashes, making you invulnerable during the duration.'
                ]
            },
            {
                hero: 'Drow Ranger',
                abilities: [
                    'Frost Arrows: Adds a freezing effect to your attacks, slowing enemy movement.',
                    'Gust: Releases a powerful gust of wind that silences and knocks back enemies.',
                    'Multishot: Fires arrows in a cone, dealing damage to multiple enemies.',
                    'Marksmanship: Grants bonus agility and attack range when no enemy heroes are nearby.'
                ]
            },
            {
                hero: 'Lion',
                abilities: [
                    'Earth Spike: Sends a spike through the ground, damaging and stunning enemy units.',
                    'Hex: Transforms an enemy unit into a harmless critter.',
                    'Mana Drain: Drains mana from an enemy, replenishing your own.',
                    'Finger of Death: Deals massive damage to an enemy unit.'
                ]
            },
            {
                hero: 'Pudge',
                abilities: [
                    'Meat Hook: Launches a bloody hook that grabs an enemy unit.',
                    'Rot: Deals damage to enemies around you at the cost of your own health.',
                    'Flesh Heap: Grants bonus magic resistance and stacks of strength for each kill.',
                    'Dismember: Suppresses and deals damage to an enemy unit over time while restoring your health.'
                ]
            },
            {
                hero: 'Lina',
                abilities: [
                    'Dragon Slave: Unleashes a wave of fire that damages enemies in a line.',
                    'Light Strike Array: Summons a pillar of flame that damages and stuns enemies.',
                    'Fiery Soul: Grants bonus attack and movement speed with each spell cast.',
                    'Laguna Blade: Fires an intense beam of lightning at a single target.'
                ]
            },
            {
                hero: 'Shadow Fiend',
                abilities: [
                    'Shadowraze: Deals damage in front of you at three different distances.',
                    'Necromastery: Collects souls from units you kill, granting bonus damage.',
                    'Presence of the Dark Lord: Reduces enemy armor around you.',
                    'Requiem of Souls: Releases collected souls in a damaging explosion around you.'
                ]
            },
            {
                hero: 'Anti-Mage',
                abilities: [
                    'Mana Break: Burns mana with each attack and deals bonus damage based on mana burned.',
                    'Blink: Teleports to a target point up to a limited distance.',
                    'Counterspell: Creates a shield that reflects targeted spells.',
                    'Mana Void: Creates a violent explosion that damages enemies based on their missing mana.'
                ]
            },
            {
                hero: 'Zeus',
                abilities: [
                    'Arc Lightning: Releases a bolt of lightning that jumps between nearby enemies.',
                    'Lightning Bolt: Summons a bolt of lightning to strike a target.',
                    'Static Field: Causes all nearby visible enemies to lose a percentage of their health when you cast a spell.',
                    'Thundergod\'s Wrath: Strikes all enemy heroes with lightning, no matter where they may be.'
                ]
            },
            {
                hero: 'Invoker',
                abilities: [
                    'Quas: Provides health regeneration and affects spells.',
                    'Wex: Provides attack speed and affects spells.',
                    'Exort: Provides damage and affects spells.',
                    'Invoke: Creates a new spell based on the combination of Quas, Wex, and Exort.'
                ]
            },
            {
                hero: 'Phantom Assassin',
                abilities: [
                    'Stifling Dagger: Throws a dagger that deals damage and slows an enemy.',
                    'Phantom Strike: Teleports to a target unit, gaining bonus attack speed.',
                    'Blur: Provides a chance to avoid physical attacks and makes you harder to see on the minimap.',
                    'Coup de Grace: Gives a chance to deal a critical strike.'
                ]
            },
            {
                hero: 'Sniper',
                abilities: [
                    'Shrapnel: Launches a shell that damages and slows enemies in an area.',
                    'Headshot: Gives a chance to deal bonus damage and mini-stun an enemy.',
                    'Take Aim: Increases your attack range.',
                    'Assassinate: Fires a devastating shot at a target enemy unit after a short aiming duration.'
                ]
            },
            {
                hero: 'Faceless Void',
                abilities: [
                    'Time Walk: Rushes forward, reversing any damage taken in the last few seconds.',
                    'Time Dilation: Freezes ability cooldowns for enemy heroes.',
                    'Timelock: Gives you a chance to lock an enemy in time, stunning them and dealing bonus damage.',
                    'Chronosphere: Creates a sphere where all units except yourself are frozen in time.'
                ]
            },
            {
                hero: 'Earthshaker',
                abilities: [
                    'Fissure: Creates an impassable line of stone that damages and stuns enemies.',
                    'Enchant Totem: Enhances your next attack with bonus damage.',
                    'Aftershock: Causes your spells to emit a damaging and stunning shockwave around you.',
                    'Echo Slam: Shakes the earth with a mighty blow, damaging nearby enemies based on how many are around.'
                ]
            }
        ];
    }

    function getTriviaMockData() {
        return [
            {
                question: 'Which of these items is NOT available in the side shop?',
                options: ['Ring of Health', 'Boots of Speed', 'Quelling Blade', 'Stout Shield'],
                correctIndex: 0,
                explanation: 'Ring of Health can only be purchased from the main shop, not the side shop.'
            },
            {
                question: 'Which hero has the lowest base movement speed in the game?',
                options: ['Crystal Maiden', 'Sniper', 'Invoker', 'Techies'],
                correctIndex: 0,
                explanation: 'Crystal Maiden has the lowest base movement speed at 275.'
            },
            {
                question: 'What is the cooldown of Black King Bar at level 1?',
                options: ['60 seconds', '70 seconds', '80 seconds', '90 seconds'],
                correctIndex: 2,
                explanation: 'Black King Bar has an 80-second cooldown when first purchased.'
            },
            {
                question: 'Which of these heroes does NOT have an Aghanim\'s Scepter upgrade?',
                options: ['Wraith King', 'Anti-Mage', 'Phantom Assassin', 'Medusa'],
                correctIndex: 2,
                explanation: 'As of this quiz creation, Phantom Assassin does not have an Aghanim\'s Scepter upgrade.'
            },
            {
                question: 'How many seconds does it take for Roshan to respawn?',
                options: ['5-10 minutes', '8-11 minutes', '10-15 minutes', '12-20 minutes'],
                correctIndex: 1,
                explanation: 'Roshan respawns between 8 and 11 minutes after being killed.'
            },
            {
                question: 'What is the maximum level a hero can reach in Dota 2?',
                options: ['25', '30', '35', '40'],
                correctIndex: 1,
                explanation: 'Heroes can reach a maximum level of 30 in Dota 2.'
            },
            {
                question: 'Which of these heroes is NOT an Intelligence hero?',
                options: ['Lina', 'Silencer', 'Lion', 'Vengeful Spirit'],
                correctIndex: 3,
                explanation: 'Vengeful Spirit is an Agility hero, while the others are Intelligence heroes.'
            },
            {
                question: 'What does BKB stand for?',
                options: ['Big Killer Blade', 'Black King Bar', 'Battle Knight Blade', 'Burning King Bar'],
                correctIndex: 1,
                explanation: 'BKB stands for Black King Bar, an item that grants magic immunity.'
            },
            {
                question: 'Which organization hosts The International Dota 2 tournament?',
                options: ['Riot Games', 'Blizzard', 'Valve', 'Epic Games'],
                correctIndex: 2,
                explanation: 'Valve Corporation is the developer of Dota 2 and organizes The International tournament.'
            },
            {
                question: 'Which of these is NOT a neutral creep camp?',
                options: ['Ancient', 'Hard', 'Medium', 'Extreme'],
                correctIndex: 3,
                explanation: 'There are Small, Medium, Hard, and Ancient neutral camps, but no Extreme camp.'
            },
            {
                question: 'How many lanes are there in a standard Dota 2 map?',
                options: ['1', '2', '3', '4'],
                correctIndex: 2,
                explanation: 'There are 3 lanes in Dota 2: top (offlane), middle, and bottom (safe lane).'
            },
            {
                question: 'Which rune spawns every 2 minutes?',
                options: ['Bounty', 'Double Damage', 'Haste', 'Illusion'],
                correctIndex: 0,
                explanation: 'Bounty runes spawn every 2 minutes, while power runes (like Double Damage) spawn every 4 minutes.'
            },
            {
                question: 'What is the cost of a Town Portal Scroll?',
                options: ['50 gold', '75 gold', '100 gold', '135 gold'],
                correctIndex: 2,
                explanation: 'A Town Portal Scroll costs 100 gold.'
            },
            {
                question: 'Which hero is known as the "Grand Magus"?',
                options: ['Invoker', 'Rubick', 'Lina', 'Zeus'],
                correctIndex: 1,
                explanation: 'Rubick is known as the Grand Magus in Dota 2 lore.'
            },
            {
                question: 'How many heroes were available in the original release of Dota 2?',
                options: ['102', '112', '117', '123'],
                correctIndex: 1,
                explanation: 'Dota 2 was released with 112 heroes.'
            }
        ];
    }

    function getItemGuesserMockData() {
        return [
            {
                name: 'Black King Bar',
                description: 'Grants magic immunity when activated, preventing most spells from affecting you.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/black_king_bar.png',
                cost: '4050',
                components: [
                    { name: 'Ogre Axe', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ogre_axe.png' },
                    { name: 'Mithril Hammer', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/mithril_hammer.png' }
                ]
            },
            {
                name: 'Blink Dagger',
                description: 'Teleports you to a target point up to 1200 units away. Blink Dagger cannot be used for 3 seconds after taking damage from an enemy hero or Roshan.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/blink.png',
                cost: '2250',
                components: []
            },
            {
                name: 'Aghanim\'s Scepter',
                description: 'Upgrades the ultimate, and sometimes other abilities, of all heroes.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ultimate_scepter.png',
                cost: '4200',
                components: [
                    { name: 'Point Booster', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/point_booster.png' },
                    { name: 'Ogre Axe', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ogre_axe.png' },
                    { name: 'Blade of Alacrity', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/blade_of_alacrity.png' },
                    { name: 'Staff of Wizardry', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/staff_of_wizardry.png' }
                ]
            },
            {
                name: 'Desolator',
                description: 'Your attacks reduce enemy armor for 7 seconds.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/desolator.png',
                cost: '3500',
                components: [
                    // Continuazione di getItemGuesserMockData() da dove si è interrotto
                    { name: 'Mithril Hammer', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/mithril_hammer.png' },
                    { name: 'Mithril Hammer', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/mithril_hammer.png' }
                ]
            },
            {
                name: 'Manta Style',
                description: 'Creates two images of your hero that deal damage. Removes negative buffs on use.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/manta.png',
                cost: '4700',
                components: [
                    { name: 'Ultimate Orb', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ultimate_orb.png' },
                    { name: 'Yasha', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/yasha.png' }
                ]
            },
            {
                name: 'Heart of Tarrasque',
                description: 'Provides enormous health regeneration when out of combat.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/heart.png',
                cost: '5150',
                components: [
                    { name: 'Reaver', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/reaver.png' },
                    { name: 'Vitality Booster', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/vitality_booster.png' },
                    { name: 'Ring of Tarrasque', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ring_of_tarrasque.png' }
                ]
            },
            {
                name: 'Divine Rapier',
                description: 'Grants enormous damage bonus but drops on death.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/rapier.png',
                cost: '5950',
                components: [
                    { name: 'Sacred Relic', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/relic.png' },
                    { name: 'Demon Edge', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/demon_edge.png' }
                ]
            },
            {
                name: 'Daedalus',
                description: 'Grants a chance to deal critical damage on an attack.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/greater_crit.png',
                cost: '5150',
                components: [
                    { name: 'Crystalys', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/lesser_crit.png' },
                    { name: 'Demon Edge', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/demon_edge.png' }
                ]
            },
            {
                name: 'Radiance',
                description: 'Burns enemies in a large radius and causes them to miss attacks.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/radiance.png',
                cost: '4700',
                components: [
                    { name: 'Sacred Relic', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/relic.png' }
                ]
            },
            {
                name: 'Refresher Orb',
                description: 'Resets the cooldowns of all your abilities and items.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/refresher.png',
                cost: '5000',
                components: [
                    { name: 'Perseverance', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/pers.png' },
                    { name: 'Perseverance', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/pers.png' }
                ]
            },
            {
                name: 'Scythe of Vyse',
                description: 'Turns an enemy into a harmless critter for a few seconds.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/sheepstick.png',
                cost: '5600',
                components: [
                    { name: 'Mystic Staff', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/mystic_staff.png' },
                    { name: 'Ultimate Orb', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ultimate_orb.png' },
                    { name: 'Void Stone', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/void_stone.png' }
                ]
            },
            {
                name: 'Eye of Skadi',
                description: 'Slows attack and movement speed of the target and reduces their healing.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/skadi.png',
                cost: '5300',
                components: [
                    { name: 'Ultimate Orb', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ultimate_orb.png' },
                    { name: 'Ultimate Orb', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/ultimate_orb.png' },
                    { name: 'Point Booster', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/point_booster.png' }
                ]
            },
            {
                name: 'Bloodthorn',
                description: 'Silences a target and causes all attacks on them to have True Strike and a critical hit.',
                icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/bloodthorn.png',
                cost: '6800',
                components: [
                    { name: 'Orchid Malevolence', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/orchid.png' },
                    { name: 'Crystalys', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/lesser_crit.png' },
                    { name: 'Soul Relic', icon: 'https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/items/soul_booster.png' }
                ]
            }
        ];
    }

    // Aggiungiamo una funzione per visualizzare una pagina iniziale
    function showHomePage() {
        // Nascondiamo tutti i contenitori di gioco
        document.getElementById('game-container').style.display = 'none';

        // Mostriamo la home page
        document.getElementById('home-container').style.display = 'block';
    }

    // Funzione per prevenire che le immagini rotte possano rovinare l'UI
    function setupImageFallbacks() {
        document.querySelectorAll('img').forEach(img => {
            img.onerror = function () {
                this.src = '/assets/images/placeholder.png';
                this.alt = 'Image not available';
            };
        });
    }

    // Funzione per aggiungere eventualmente statistiche di gioco
    function saveGameStats() {
        // Implementazione futura: salvare i punteggi in localStorage
        const gameStats = {
            heroQuiz: {
                highScore: Math.max(quizScore, parseInt(localStorage.getItem('heroQuizHighScore') || '0')),
                gamesPlayed: parseInt(localStorage.getItem('heroQuizGamesPlayed') || '0') + 1
            },
            triviaGame: {
                highScore: Math.max(triviaScore, parseInt(localStorage.getItem('triviaHighScore') || '0')),
                gamesPlayed: parseInt(localStorage.getItem('triviaGamesPlayed') || '0') + 1
            },
            itemGuesser: {
                highScore: Math.max(itemScore, parseInt(localStorage.getItem('itemGuesserHighScore') || '0')),
                gamesPlayed: parseInt(localStorage.getItem('itemGuesserGamesPlayed') || '0') + 1
            }
        };

        // Salviamo i nuovi punteggi
        if (currentGame === 'hero-quiz') {
            localStorage.setItem('heroQuizHighScore', gameStats.heroQuiz.highScore);
            localStorage.setItem('heroQuizGamesPlayed', gameStats.heroQuiz.gamesPlayed);
        } else if (currentGame === 'trivia-game') {
            localStorage.setItem('triviaHighScore', gameStats.triviaGame.highScore);
            localStorage.setItem('triviaGamesPlayed', gameStats.triviaGame.gamesPlayed);
        } else if (currentGame === 'item-guesser-game') {
            localStorage.setItem('itemGuesserHighScore', gameStats.itemGuesser.highScore);
            localStorage.setItem('itemGuesserGamesPlayed', gameStats.itemGuesser.gamesPlayed);
        }
    }

    // Inizializzazione dell'app
    function initApp() {
        // Configurazione iniziale
        setupImageFallbacks();

        // Caricamento dei dati di gioco all'avvio
        heroQuizData = getHeroQuizMockData();
        triviaData = getTriviaMockData();
        itemGuesserData = getItemGuesserMockData();

        // Mostra la home page all'avvio
        showHomePage();
    }

    // Eseguiamo l'inizializzazione quando il DOM è completamente caricato
    initApp();
});