extends Label

enum {BOTTOM, RIGHT}
var location = BOTTOM

var char_width_px = 20
var num_rows = 8
var pt_size = 32

var top_border = "=="
var space = ""

onready var dialogue_box_text = $DialogueBoxText

# helper func to calculate width of dialogue box in units of char-widths
func get_desired_width():
	if self.location == BOTTOM:
		return get_viewport().size.x/char_width_px
	elif self.location == RIGHT:
		var box_width_px = get_viewport().size.x/3
		return box_width_px/char_width_px
	

# generate string for dialogue box
func generate_border():

	top_border = "=="
	space = ""
	
	var new_border = ""
	
	var width_char = get_desired_width() 
	
	for i in range(width_char - 2):
		top_border += "="
		space += " "
	
	new_border = top_border
	
	for i in range(num_rows):
		new_border += "\n|" + space + "|"	
	
	new_border += "\n" + top_border
	
	self.text = new_border

# update hitbox
func update_size():
	self.rect_size.x = len(top_border)*char_width_px
	self.rect_size.y = len(self.text)/len(top_border)*pt_size + 80

# put dialogue box at bottom
func update_position(win_width, win_height):
	if location == BOTTOM:
		self.rect_position.x = (win_width - self.rect_size.x)/2
		self.rect_position.y = win_height - self.rect_size.y - pt_size - 15
	
	elif location == RIGHT:
		self.rect_position.x = win_width - self.rect_size.x - char_width_px*2
		self.rect_position.y = 0 + pt_size

# set box to be on bottom (event) or right (map)
# location should be DialogueBox.BOTTOM or DialogueBox.RIGHT
func set_box_location(location):
	self.location = location

# call to show dialogue box with given description and options
func show_dialogue(description, options):
	self.dialogue_box_text.set_text(description)
	
	self.show()

# call to hide dialogue when no longer needed (cleanup)
func hide_dialogue():
	self.text = ""
	self.hide()

# Called when the node enters the scene tree for the first time.
func _ready():
	self.align = Label.ALIGN_CENTER
	self.show_dialogue("some funny", [])

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	var viewport_size = get_viewport().size
	var win_width = viewport_size.x
	var win_height = viewport_size.y

	generate_border()
	update_size()
	update_position(win_width, win_height)


