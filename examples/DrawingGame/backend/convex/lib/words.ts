// Word selection helper functions

export function selectWords(difficulty: string, count: number): string[] {
  const easyWords = [
    "cat", "dog", "house", "tree", "sun", "car", "book", "cup",
    "hat", "ball", "fish", "bird", "star", "moon", "rain", "snow"
  ];

  const mediumWords = [
    "guitar", "bicycle", "computer", "rainbow", "elephant", "volcano",
    "penguin", "telephone", "umbrella", "butterfly", "waterfall", "rocket",
    "piano", "mountain", "island", "triangle", "octopus", "dragon"
  ];

  const hardWords = [
    "astronaut", "skyscraper", "microscope", "algorithm", "constellation",
    "helicopter", "submarine", "telescope", "cathedral", "encyclopedia",
    "rollercoaster", "trampoline", "refrigerator", "kaleidoscope"
  ];

  const pool = difficulty === "easy" ? easyWords :
               difficulty === "medium" ? mediumWords :
               difficulty === "hard" ? hardWords :
               [...easyWords, ...mediumWords, ...hardWords];

  // Shuffle and take
  return pool.sort(() => Math.random() - 0.5).slice(0, count);
}

export function generateRoomCode(): string {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
  let code = "";
  for (let i = 0; i < 6; i++) {
    code += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return code;
}
