package bootstrap

type StateStore interface {
	Read() (State, error)
	Write(State) error
}
